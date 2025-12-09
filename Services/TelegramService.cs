using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using MemosToBarkOrTelegram.Models;

namespace MemosToBarkOrTelegram.Services
{
    public interface ITelegramService
    {
        Task<TelegramSendResult> SendMessageAsync(string message, string creatorUserId, string? url = null);
    }

    public class TelegramSendResult
    {
        public int TotalRecipients { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class TelegramService(HttpClient httpClient, IOptions<TelegramOptions> options, ILogger<TelegramService> logger) : ITelegramService
    {
        private readonly TelegramOptions _options = options.Value;

        public async Task<TelegramSendResult> SendMessageAsync(string message, string creatorUserId, string? url = null)
        {
            var result = new TelegramSendResult();

            if (!_options.Enabled)
            {
                logger.LogDebug("Telegram notification is disabled");
                return result;
            }

            if (string.IsNullOrEmpty(_options.BotToken))
            {
                logger.LogWarning("Telegram BotToken is not configured");
                return result;
            }

            // Get chat IDs for this user (excluding creator's own chats)
            var chatIds = _options.GetChatIdsForUser(creatorUserId);

            if (chatIds.Count == 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("No Telegram chats configured for notification (creator: {CreatorUserId})", creatorUserId);
                return result;
            }

            result.TotalRecipients = chatIds.Count;

            var fullMessage = message;
            if (!string.IsNullOrEmpty(url))
            {
                fullMessage += $"\n\n<a href=\"{HttpUtility.HtmlEncode(url)}\">Detail</a>";
            }

            // Send to all chat IDs in parallel
            var sendTasks = chatIds.Select(chatId => 
                SendToChatAsync(chatId, fullMessage)).ToArray();

            var results = await Task.WhenAll(sendTasks);

            result.SuccessCount = results.Count(r => r);
            result.FailureCount = results.Count(r => !r);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Telegram message sent to {Total} chats: {Success} succeeded, {Failed} failed",
                    result.TotalRecipients, result.SuccessCount, result.FailureCount);

            return result;
        }

        private async Task<bool> SendToChatAsync(string chatId, string message)
        {
            try
            {
                var payload = new Dictionary<string, object>
                {
                    ["chat_id"] = chatId,
                    ["text"] = message,
                    ["parse_mode"] = _options.ParseMode,
                    ["disable_web_page_preview"] = true
                };

                var requestUrl = $"https://api.telegram.org/bot{_options.BotToken}/sendMessage";
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await httpClient.PostAsync(requestUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Telegram message sent successfully to chat: {ChatId}", chatId);
                    return true;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning("Failed to send Telegram message to chat {ChatId}: {StatusCode} - {Response}",
                        chatId, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(ex, "Error sending Telegram message to chat: {ChatId}", chatId);
                return false;
            }
        }
    }
}
