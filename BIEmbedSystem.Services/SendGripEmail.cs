using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BIEmbedSystem.Services
{
    public class SendGripEmail
    {
        private readonly string _apiKey;

        public SendGripEmail(IConfiguration configuration)
        {
            _apiKey = configuration["EmailSettings:Password"]; // Move API key to settings
        }

        public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string plainText, string htmlContent)
        {
            var client = new SendGridClient(_apiKey);

            var from = new EmailAddress("reporting_poc@itconvergence.com", "Reporting POC");
            var to = new EmailAddress(toEmail, toName);

            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainText, htmlContent);
            var response = await client.SendEmailAsync(msg);

            return response.IsSuccessStatusCode;
        }
    }
}
