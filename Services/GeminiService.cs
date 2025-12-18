using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Data;
using GymManagementSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;

namespace GymManagementSystem.Services
{
    public interface IOpenAIService
    {
        Task<string> GetFitnessAdviceAsync(string userMessage, string? userHeight = null, string? userWeight = null, string? bodyType = null);
        Task<string> AnalyzeBodyPhotoAsync(string imageBase64, string userMessage);
        Task<List<ChatMessage>> GetConversationHistoryAsync(string userId);
        Task SaveConversationMessageAsync(string userId, string role, string content);
    }

    public class GeminiService : IOpenAIService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;
        private const string ModelName = "gemini-flash-latest";
        private static readonly object _rateLock = new();
        private static readonly Queue<DateTime> _requestLog = new();
        private const int MaxRequestsPerMinute = 60;

        public GeminiService(IConfiguration configuration, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI:ApiKey bulunamadı.");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetFitnessAdviceAsync(string userMessage, string? userHeight = null, string? userWeight = null, string? bodyType = null)
        {
            var systemPrompt = @"Sen profesyonel bir fitness koçu ve diyetisyensin. 
Türkçe konuşuyorsun ve kullanıcılara:
- Egzersiz programları öneriyorsun
- Diyet planları hazırlıyorsun
- Motivasyon sağlıyorsun
- Vücut geliştirme tavsiyeleri veriyorsun
Cevapların detaylı, bilimsel ve motive edici olmalı.";

            var userContext = string.Empty;
            if (!string.IsNullOrWhiteSpace(userHeight) && !string.IsNullOrWhiteSpace(userWeight))
            {
                userContext = $"\n\nKullanıcı Bilgileri:\n- Boy: {userHeight} cm\n- Kilo: {userWeight} kg";
                if (!string.IsNullOrWhiteSpace(bodyType))
                {
                    userContext += $"\n- Vücut Tipi: {bodyType}";
                }
            }

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = $"{userMessage}{userContext}" }
                        }
                    }
                }
            };

            return await SendRequestAsync(payload);
        }

        public async Task<string> AnalyzeBodyPhotoAsync(string imageBase64, string userMessage)
        {
            var systemPrompt = @"Sen bir fitness uzmanısın. Kullanıcının yüklediği fotoğrafa bakarak:
1. Vücut kompozisyonunu analiz et (kas/yağ oranı tahmini)
2. Güçlü ve geliştirilmesi gereken bölgeleri belirle
3. Özel egzersiz önerileri sun
4. Motivasyon ver
Türkçe, nazik ve profesyonel bir dille cevap ver.";

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = userMessage },
                            new
                            {
                                inlineData = new
                                {
                                    data = imageBase64,
                                    mimeType = "image/jpeg"
                                }
                            }
                        }
                    }
                }
            };

            return await SendRequestAsync(payload);
        }

        public async Task<List<ChatMessage>> GetConversationHistoryAsync(string userId)
        {
            var history = await _context.AIConversations
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.CreatedAt)
                .Take(20)
                .ToListAsync();

            var messages = new List<ChatMessage>();
            foreach (var msg in history)
            {
                if (msg.Role == "assistant")
                {
                    messages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(msg.Content));
                }
                else
                {
                    messages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(msg.Content));
                }
            }

            return messages;
        }

        public async Task SaveConversationMessageAsync(string userId, string role, string content)
        {
            var record = new AIConversation
            {
                UserId = userId,
                Role = role,
                Content = content,
                CreatedAt = DateTime.Now
            };

            _context.AIConversations.Add(record);
            await _context.SaveChangesAsync();
        }

        private async Task<string> SendRequestAsync(object payload)
        {
            await EnforceRateLimitAsync();

            try
            {
                var requestUri = $"https://generativelanguage.googleapis.com/v1/models/{ModelName}:generateContent?key={_apiKey}";
                using var response = await _httpClient.PostAsJsonAsync(requestUri, payload);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"Gemini API hatası: {json}");
                }

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var content = root.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return content ?? "AI cevabı alınamadı.";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Gemini servisi hata verdi: {ex.Message}", ex);
            }
        }

        private static async Task EnforceRateLimitAsync()
        {
            TimeSpan window = TimeSpan.FromMinutes(1);
            TimeSpan delay = TimeSpan.Zero;

            lock (_rateLock)
            {
                var now = DateTime.UtcNow;
                while (_requestLog.Count > 0 && now - _requestLog.Peek() > window)
                {
                    _requestLog.Dequeue();
                }

                if (_requestLog.Count >= MaxRequestsPerMinute)
                {
                    var wait = window - (now - _requestLog.Peek());
                    if (wait > TimeSpan.Zero)
                    {
                        delay = wait;
                    }
                }

                if (delay == TimeSpan.Zero)
                {
                    _requestLog.Enqueue(DateTime.UtcNow);
                }
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
                lock (_rateLock)
                {
                    _requestLog.Enqueue(DateTime.UtcNow);
                }
            }
        }
    }
}
