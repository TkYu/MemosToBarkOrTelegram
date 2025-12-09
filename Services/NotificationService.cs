using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MemosToBarkOrTelegram.Models;

namespace MemosToBarkOrTelegram.Services
{
    public interface INotificationService
    {
        Task ForwardMemoAsync(MemosWebhookPayload payload);
    }

    public class NotificationService(
        IBarkService barkService,
        ITelegramService telegramService,
        IOptions<MemosOptions> memosOptions,
        ILogger<NotificationService> logger)
        : INotificationService
    {
        private readonly MemosOptions _memosOptions = memosOptions.Value;

        public async Task ForwardMemoAsync(MemosWebhookPayload payload)
        {
            if (payload.Memo == null)
            {
                logger.LogWarning("Received webhook payload without memo content");
                return;
            }

            // Build memo URL
            var memoUrl = _memosOptions.BuildMemoUrl(payload.Memo.Name);

            // Get username from creator ID
            var creatorName = _memosOptions.GetUserName(payload.Creator);

            var (title, body) = FormatMessage(payload, creatorName);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Forwarding memo: ActivityType={ActivityType}, Creator={CreatorName} (ID: {CreatorId})",
                    payload.ActivityType, creatorName, payload.Creator);

            // Send to both services in parallel, passing creatorId to filter recipients
            var barkTask = barkService.SendNotificationAsync(title, body, payload.Creator, memoUrl);
            var telegramTask = telegramService.SendMessageAsync(
                FormatTelegramMessage(title, body, payload.Memo), payload.Creator, memoUrl);

            // Wait for both to complete, but ignore results
            await Task.WhenAll(barkTask, telegramTask);
        }

        private (string title, string body) FormatMessage(MemosWebhookPayload payload, string creatorName)
        {
            var memo = payload.Memo!;
            var template = _memosOptions.GetTemplate(payload.ActivityType);

            // Build replacements dictionary
            var replacements = new Dictionary<string, string>
            {
                ["creator"] = creatorName,
                ["content"] = memo.Content,
                ["createTime"] = FormatDateTime(memo.CreateTime),
                ["updateTime"] = FormatDateTime(memo.UpdateTime),
                ["visibility"] = GetVisibilityText(memo.Visibility)
            };

            string title, body;

            if (template != null)
            {
                // Use template
                title = template.FormatTitle(replacements);
                body = template.FormatBody(replacements);
            }
            else
            {
                // Fallback to default format
                var activityType = payload.ActivityType switch
                {
                    "memo.created" => "New Memo",
                    "memo.updated" => "Memo Updated",
                    "memo.deleted" => "Memo Deleted",
                    _ => "Memo Notify"
                };

                title = activityType;
                body = memo.Content;
            }

            // Truncate body if too long for Bark
            if (body.Length > 200)
            {
                body = body[..197] + "...";
            }

            return (title, body);
        }

        private static string FormatTelegramMessage(string title, string body, MemoInfo memo)
        {
            var message = $"<b>{System.Web.HttpUtility.HtmlEncode(title)}</b>\n\n{System.Web.HttpUtility.HtmlEncode(body)}";

            // Extract tags from content (e.g., #important)
            var tags = ExtractTags(memo.Content);
            if (tags.Count > 0)
            {
                var tagString = string.Join(" ", tags.Select(t => $"#{t}"));
                message += $"\n\n{tagString}";
            }

            // Add timestamp
            if (memo.CreateTime != null)
            {
                var createTime = FormatDateTime(memo.CreateTime);
                message += $"\n\n<i>{createTime}</i>";
            }

            return message;
        }

        private static List<string> ExtractTags(string content)
        {
            var tags = new List<string>();
            var matches = Regex.Matches(content, @"#([^\s#]+)");

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    tags.Add(match.Groups[1].Value);
                }
            }

            return tags;
        }

        /// <summary>
        /// Format TimeSpanItem to datetime string
        /// </summary>
        private static string FormatDateTime(TimeSpanItem? timeSpan)
        {
            if (timeSpan == null)
                return string.Empty;

            DateTime dateTime = timeSpan;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Convert visibility int to text
        /// </summary>
        private static string GetVisibilityText(int visibility)
        {
            return visibility switch
            {
                0 => "PRIVATE",
                1 => "PROTECTED",
                2 => "PUBLIC",
                _ => visibility.ToString(CultureInfo.InvariantCulture)
            };
        }
    }
}