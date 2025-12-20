using System.Net.Http;
using System.Text;
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
        private const string GeminiModel = "gemini-1.5-flash-latest";
        private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com";

        public GeminiService(IConfiguration configuration, ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _apiKey = configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI:ApiKey bulunamadı.");
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<string> GetFitnessAdviceAsync(string userMessage, string? userHeight = null, string? userWeight = null, string? bodyType = null)
        {
            try
            {
                var systemPrompt = "Sen profesyonel bir fitness koçu ve diyetisyensin. Türkçe konuşuyorsun ve kullanıcılara egzersiz programları öneriyorsun, diyet planları hazırlıyorsun, motivasyon sağlıyorsun ve vücut geliştirme tavsiyeleri veriyorsun. Cevapların detaylı, bilimsel ve motive edici olmalı.";

                var userContext = "";
                if (!string.IsNullOrEmpty(userHeight) && !string.IsNullOrEmpty(userWeight))
                {
                    userContext = $"\n\nKullanıcı Bilgileri:\n- Boy: {userHeight} cm\n- Kilo: {userWeight} kg";
                    if (!string.IsNullOrEmpty(bodyType))
                        userContext += $"\n- Vücut Tipi: {bodyType}";
                }

                var fullMessage = systemPrompt + "\n\n" + userMessage + userContext;

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new[] { new { text = fullMessage } }
                        }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{GeminiBaseUrl}/{GeminiApiVersion}/models/{GeminiModel}:generateContent?key={_apiKey}",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Gemini API Hatası: {responseContent}";
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var text = jsonResponse.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                await SaveConversationMessageAsync(userMessage, "user", userMessage);
                await SaveConversationMessageAsync(userMessage, "assistant", text ?? "Cevap alınamadı.");

                return text ?? "AI'dan cevap alınamadı.";
            }
            catch (Exception ex)
            {
                return $"❌ Hata: {ex.Message}";
            }
        }

        public async Task<string> AnalyzeBodyPhotoAsync(string imageBase64, string userMessage)
        {
            try
            {
                var systemPrompt = "Sen bir fitness uzmanısın. Kullanıcının yüklediği fotoğrafa bakarak: 1. Vücut kompozisyonunu analiz et (kas/yağ oranı tahmini), 2. Güçlü ve geliştirilmesi gereken bölgeleri belirle, 3. Özel egzersiz önerileri sun, 4. Motivasyon ver. Türkçe, nazik ve profesyonel bir dille cevap ver.";

                var fullMessage = systemPrompt + "\n\n" + userMessage;

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            role = "user",
                            parts = new object[]
                            {
                                new { text = fullMessage },
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

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(
                    $"{GeminiBaseUrl}/{GeminiApiVersion}/models/{GeminiModel}:generateContent?key={_apiKey}",
                    content
                );

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"❌ Gemini API Hatası: {responseContent}";
                }

                var jsonResponse = JsonDocument.Parse(responseContent);
                var text = jsonResponse.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "AI'dan fotoğraf analizi alınamadı.";
            }
            catch (Exception ex)
            {
                return $"❌ Hata: {ex.Message}";
            }
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
                    messages.Add(ChatMessage.CreateAssistantMessage(msg.Content));
                else
                    messages.Add(ChatMessage.CreateUserMessage(msg.Content));
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
    }
}
