using Microsoft.AspNetCore.Http.HttpResults;

namespace MemosToBarkOrTelegram.Models
{
    public class MemosOptions
    {
        public const string SectionName = "Memos";

        public string BaseUrl { get; set; } = "http://localhost:8080";
        
        /// <summary>
        /// User ID to username mapping for display
        /// Format: { "1": "User1", "2": "User2" }
        /// </summary>
        public Dictionary<string, string>? UserNameMap { get; set; }

        /// <summary>
        /// Message templates indexed by event type
        /// Format: { "memos.memo.created": { ... }, "memos.memo.updated": { ... } }
        /// </summary>
        public Dictionary<string, MessageTemplate>? MessageTemplates { get; set; }

        /// <summary>
        /// Get username by user ID from the mapping
        /// </summary>
        public string GetUserName(string userId)
        {
            var extract = userId;
            if (userId.Contains('/'))
            {
                var parts = userId.Split('/');
                extract = parts[^1];
            }
            if (UserNameMap == null || UserNameMap.Count == 0)
                return userId;
            return UserNameMap.GetValueOrDefault(extract, extract);
        }

        /// <summary>
        /// Get message template by event type
        /// </summary>
        public MessageTemplate? GetTemplate(string eventType)
        {
            if (MessageTemplates == null || MessageTemplates.Count == 0)
                return null;

            MessageTemplates.TryGetValue(eventType, out var template);
            return template;
        }

        /// <summary>
        /// Build memo URL from memo name (e.g., "memos/123")
        /// </summary>
        public string BuildMemoUrl(string memoName)
        {
            // Extract memo ID from name (e.g., "memos/123" -> "123")
            var parts = memoName.Split('/');
            var memoId = parts.Length > 1 ? parts[^1] : memoName;

            return $"{BaseUrl.TrimEnd('/')}/m/{memoId}";
        }
    }

    public class MessageTemplate
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Apply template with placeholder replacements
        /// </summary>
        private static string ApplyTemplate(string template, Dictionary<string, string> replacements)
        {
            return replacements.Aggregate(template, (current, kvp) => current.Replace($"{{{kvp.Key}}}", kvp.Value));
        }

        /// <summary>
        /// Format title with replacements
        /// </summary>
        public string FormatTitle(Dictionary<string, string> replacements)
        {
            return ApplyTemplate(Title, replacements);
        }

        /// <summary>
        /// Format body with replacements
        /// </summary>
        public string FormatBody(Dictionary<string, string> replacements)
        {
            return ApplyTemplate(Body, replacements);
        }
    }
}
