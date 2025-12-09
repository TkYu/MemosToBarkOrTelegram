using System.Text.Json.Serialization;

namespace MemosToBarkOrTelegram.Models
{
    /// <summary>
    /// Memos webhook payload (v0.25.3)
    /// </summary>
    public class MemosWebhookPayload
    {
        [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

        [JsonPropertyName("activityType")] public string ActivityType { get; set; } = string.Empty;

        [JsonPropertyName("creator")] public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("memo")] public MemoInfo? Memo { get; set; }
    }

    public class MemoInfo
    {
        [JsonPropertyName("state")] public int State { get; set; }

        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("creator")] //why?
        public string Creator { get; set; } = string.Empty;

        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;

        [JsonPropertyName("create_time")] public TimeSpanItem? CreateTime { get; set; }

        [JsonPropertyName("update_time")] public TimeSpanItem? UpdateTime { get; set; }

        [JsonPropertyName("display_time")] public TimeSpanItem? DisplayTime { get; set; }

        [JsonPropertyName("visibility")] public int Visibility { get; set; }
    }

    public class TimeSpanItem
    {
        [JsonPropertyName("seconds")] public int Seconds { get; set; }

        public static implicit operator DateTimeOffset(TimeSpanItem item)
        {
            return DateTimeOffset.FromUnixTimeSeconds(item.Seconds);
        }

        public static implicit operator DateTime(TimeSpanItem item)
        {
            return DateTimeOffset.FromUnixTimeSeconds(item.Seconds).DateTime;
        }
    }
}