using System.Collections.Generic;
using GymManagementSystem.Models.Entities;

namespace GymManagementSystem.Models.ViewModels
{
    public class AIAssistantViewModel
    {
        public List<AIConversation> ConversationHistory { get; set; } = new();
    }

    public class AIMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? ImageBase64 { get; set; }
        public string? Height { get; set; }
        public string? Weight { get; set; }
        public string? BodyType { get; set; }
    }
}
