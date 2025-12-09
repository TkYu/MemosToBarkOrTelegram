namespace MemosToBarkOrTelegram.Models
{
    public class TelegramOptions
    {
        public const string SectionName = "Telegram";

        public bool Enabled { get; set; }
        public string BotToken { get; set; } = string.Empty;

        /// <summary>
        /// User ID to chat IDs mapping
        /// Format: { "1": ["123456789"], "2": ["987654321", "-1001234567890"] }
        /// Use "*" as key for global chats that receive all notifications
        /// </summary>
        public Dictionary<string, string[]>? ChatIds { get; set; }

        public string ParseMode { get; set; } = "HTML";

        /// <summary>
        /// Get chat IDs for a specific user, excluding their own chats
        /// </summary>
        /// <param name="creatorUserId">The user ID who created the memo</param>
        /// <returns>List of chat IDs that should receive the notification</returns>
        public List<string> GetChatIdsForUser(string creatorUserId)
        {
            var result = new List<string>();

            if (ChatIds == null || ChatIds.Count == 0)
                return result;

            // Add global chats (for everyone)
            if (ChatIds.TryGetValue("*", out var globalChats))
            {
                result.AddRange(globalChats);
            }

            // Add chats for all users except the creator
            foreach (var kvp in ChatIds.Where(kvp => kvp.Key != "*" && kvp.Key != creatorUserId))
            {
                result.AddRange(kvp.Value);
            }

            return result;
        }
    }

}