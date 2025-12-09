using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Options;
using MemosToBarkOrTelegram.Models;

namespace MemosToBarkOrTelegram.Services
{
    public interface IBarkService
    {
        Task<BarkSendResult> SendNotificationAsync(string title, string body, string creatorUserId, string? url = null);
    }

    public class BarkSendResult
    {
        public int TotalRecipients { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
    }

    public class BarkService(HttpClient httpClient, IOptions<BarkOptions> options, ILogger<BarkService> logger) : IBarkService
    {
        private readonly BarkOptions _options = options.Value;

        public async Task<BarkSendResult> SendNotificationAsync(string title, string body, string creatorUserId, string? url = null)
        {
            var result = new BarkSendResult();

            if (!_options.Enabled)
            {
                logger.LogDebug("Bark notification is disabled");
                return result;
            }

            // Get device keys for this user (excluding creator's own devices)
            var deviceKeys = _options.GetDeviceKeysForUser(creatorUserId);

            if (deviceKeys.Count == 0)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                    logger.LogDebug("No Bark devices configured for notification (creator: {CreatorUserId})", creatorUserId);
                return result;
            }

            result.TotalRecipients = deviceKeys.Count;

            // Send to all device keys in parallel
            var sendTasks = deviceKeys.Select(deviceKey => 
                SendToDeviceAsync(deviceKey, title, body, url)).ToArray();

            var results = await Task.WhenAll(sendTasks);

            result.SuccessCount = results.Count(r => r);
            result.FailureCount = results.Count(r => !r);

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Bark notification sent to {Total} devices: {Success} succeeded, {Failed} failed",
                    result.TotalRecipients, result.SuccessCount, result.FailureCount);

            return result;
        }

        private async Task<bool> SendToDeviceAsync(string deviceKey, string title, string body, string? url)
        {
            try
            {
                var formPairs = new List<KeyValuePair<string, string>>
                {
                    new("title", title),
                    new("body", body),
                    new("device_key", deviceKey)
                };

                if (!string.IsNullOrEmpty(_options.Group))
                    formPairs.Add(new KeyValuePair<string, string>("group", _options.Group));

                if (!string.IsNullOrEmpty(_options.Icon))
                    formPairs.Add(new KeyValuePair<string, string>("icon", _options.Icon));

                if (!string.IsNullOrEmpty(_options.Sound))
                    formPairs.Add(new KeyValuePair<string, string>("sound", _options.Sound));

                if (!string.IsNullOrEmpty(url))
                    formPairs.Add(new KeyValuePair<string, string>("url", url));

                using var content = new FormUrlEncodedContent(formPairs);

                content.Headers.ContentType!.CharSet = "utf-8";
                
                var response = await httpClient.PostAsync("push", content);

                if (response.IsSuccessStatusCode)
                {
                    if(logger.IsEnabled(LogLevel.Debug)) 
                        logger.LogDebug("Bark notification sent successfully to device: {DeviceKey}", deviceKey);
                    return true;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                if (logger.IsEnabled(LogLevel.Warning))
                    logger.LogWarning("Failed to send Bark notification to device {DeviceKey}: {StatusCode} - {Response}",
                        deviceKey, response.StatusCode, responseBody);
                return false;
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                    logger.LogError(ex, "Error sending Bark notification to device: {DeviceKey}", deviceKey);
                return false;
            }
        }
    }
}