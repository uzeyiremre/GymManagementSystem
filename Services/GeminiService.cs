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

        private const string GeminiApiVersion = "v1beta";
        private const string GeminiModel = "gemini-2.5-flash";
        private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com";

        // Basit hız sınırlama
        private static readonly object _rateLock = new();
        private static readonly Queue<DateTime> _requestLog = new();
        private const int MaxRequestsPerMinute = 15;

        public GeminiService(IConfiguration configuration, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI:ApiKey bulunamadı.");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetFitnessAdviceAsync(string userMessage, string? userHeight = null, string? userWeight = null, string? bodyType = null)
        {
            var systemPromptText = @"Sen profesyonel bir fitness koçu ve diyetisyensin.
Türkçe konuşuyorsun ve kullanıcılara:
- Egzersiz programları öneriyorsun
- Diyet planları hazırlıyorsun
- Motivasyon sağlıyorsun
- Vücut geliştirme tavsiyeleri veriyorsun
Cevapların detaylı, bilimsel ve motive edici olmalı.";

            var contextInfo = string.Empty;
            if (!string.IsNullOrWhiteSpace(userHeight) && !string.IsNullOrWhiteSpace(userWeight))
            {
                contextInfo = $"\n\n[Kullanıcı Verileri -> Boy: {userHeight} cm, Kilo: {userWeight} kg";
                if (!string.IsNullOrWhiteSpace(bodyType))
                {
                    contextInfo += $", Vücut Tipi: {bodyType}";
                }
                contextInfo += "]";
            }

            var finalUserMessage = userMessage + contextInfo;

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new object[]
                    {
                        new { text = systemPromptText }
                    }
                },
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = finalUserMessage }
                        }
                    }
                }
            };

            return await SendGeminiRequestAsync(payload);
        }

        public async Task<string> AnalyzeBodyPhotoAsync(string imageBase64, string userMessage)
        {
            var systemPromptText = @"Sen uzman bir vücut geliştirme antrenörüsün.
Gönderilen fotoğrafı analiz et:
1. Vücut yağ oranı tahmini yap.
2. Kas kütlesi ve simetri durumunu değerlendir.
3. Eksik bölgeleri tespit et.
4. Bu kişiye özel tavsiyeler ver.
Yanıtın Türkçe, profesyonel ve yapıcı olsun.";

            var payload = new
            {
                systemInstruction = new
                {
                    parts = new object[]
                    {
                        new { text = systemPromptText }
                    }
                },
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = userMessage },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = imageBase64
                                }
                            }
                        }
                    }
                }
            };

            return await SendGeminiRequestAsync(payload);
        }

        private async Task<string> SendGeminiRequestAsync(object payload)
        {
            await EnforceRateLimitAsync();

            var requestUri = $"{GeminiBaseUrl}/{GeminiApiVersion}/models/{GeminiModel}:generateContent?key={_apiKey}";

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            using var response = await _httpClient.PostAsJsonAsync(requestUri, payload, jsonOptions);
            var jsonResponse = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Gemini API Hatası ({response.StatusCode}): {jsonResponse}");
            }

            using var document = JsonDocument.Parse(jsonResponse);

            if (document.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var content) &&
                    content.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var text = parts[0].GetProperty("text").GetString();
                    return text ?? "Cevap metni boş döndü.";
                }
            }

            return "AI mantıklı bir cevap oluşturamadı.";
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
                messages.Add(msg.Role == "assistant"
                    ? ChatMessage.CreateAssistantMessage(msg.Content)
                    : ChatMessage.CreateUserMessage(msg.Content));
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

        private static async Task EnforceRateLimitAsync()
        {
            var window = TimeSpan.FromMinutes(1);
            var delay = TimeSpan.Zero;

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
