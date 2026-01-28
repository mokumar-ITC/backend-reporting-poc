using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<NotificationController> _logger;
        private readonly IConfiguration _config;

        public NotificationController(
            EmailService emailService,
            SendGripEmail emailServicev1,
            EmailServiceGraph emailServicev2,
            ILogger<NotificationController> logger,
            IConfiguration config)
        {
            _emailService = emailService;
            _logger = logger;
            _config = config;
        }

        /// <summary>
        /// Sends a test notification email using the configured SMTP settings.
        /// If using Ethereal or Mailtrap, returns the preview inbox link.
        /// </summary>
        [HttpPost("send-email")]
        public async Task<IActionResult> SendEmail([FromBody] List<string> recipients)
        {
            if (recipients == null || recipients.Count == 0)
                return BadRequest("Recipient list cannot be empty.");

            string subject = "🔔 Test Notification from Reporting Hub";
            string body = "<h3>Hello!</h3><p>This is a test email from your Reporting Hub.</p>";

            try
            {
                await _emailService.SendEmailAsync(recipients, subject, body);

                // Determine which inbox URL to return based on SMTP settings
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? string.Empty;
                string? inboxUrl = null;

                if (smtpServer.Contains("ethereal.email", StringComparison.OrdinalIgnoreCase))
                {
                    inboxUrl = "https://ethereal.email/messages";
                }
                else if (smtpServer.Contains("mailtrap.io", StringComparison.OrdinalIgnoreCase))
                {
                    inboxUrl = "https://mailtrap.io/inboxes";
                }

                _logger.LogInformation("✅ Email sent to: {Recipients}", string.Join(", ", recipients));

                return Ok(new
                {
                    message = "✅ Email sent successfully!",
                    provider = smtpServer,
                    inboxPreview = inboxUrl ?? "No preview available (real SMTP server)."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to send test email");
                return StatusCode(500, new { error = "Failed to send email", details = ex.Message });
            }
        }
    }
}
