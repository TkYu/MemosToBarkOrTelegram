using Microsoft.AspNetCore.Mvc;
using MemosToBarkOrTelegram.Models;
using MemosToBarkOrTelegram.Services;

namespace MemosToBarkOrTelegram.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController(INotificationService notificationService, ILogger<WebhookController> logger) : ControllerBase
    {
        /// <summary>
        /// Receives webhook notifications from Memos
        /// </summary>
        /// <param name="payload">The Memos webhook payload</param>
        /// <returns>Result of the notification forwarding</returns>
        [HttpPost("memos")]
        public async Task<IActionResult> ReceiveMemosWebhook([FromBody] MemosWebhookPayload payload)
        {
            // var requestString = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            // var payload = System.Text.Json.JsonSerializer.Deserialize<MemosWebhookPayload>(requestString);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Received Memos webhook: ActivityType={ActivityType}", payload.ActivityType);
            try
            {
                await notificationService.ForwardMemoAsync(payload);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing Memos webhook");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "g2g", timestamp = DateTime.UtcNow });
        }
    }
}