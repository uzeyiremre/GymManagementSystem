namespace OpenAI.Chat
{
    public class ChatMessage
    {
        public string Role { get; }
        public string Content { get; }

        private ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        public static ChatMessage CreateUserMessage(string content) => new("user", content);

        public static ChatMessage CreateAssistantMessage(string content) => new("assistant", content);
    }
}
