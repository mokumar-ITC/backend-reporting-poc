// EmailService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MailKit.Security; // only if you still use MailKit elsewhere; not necessary here
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;
using BIEmbedSystem.Core.Entities;

namespace BIEmbedSystem.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        private const string GmailTokenEndpoint = "https://oauth2.googleapis.com/token";
        private const string GmailSendEndpoint = "https://gmail.googleapis.com/gmail/v1/users/me/messages/send";

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _settings = settings.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var _ = new MailAddress(email);
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            catch
            {
                _logger.LogWarning("Invalid email skipped: {Email}", email);
                return false;
            }
        }

        // Exchange refresh token for access token
        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_settings.GmailClientId) ||
                string.IsNullOrEmpty(_settings.GmailClientSecret) ||
                string.IsNullOrEmpty(_settings.GmailRefreshToken))
            {
                throw new InvalidOperationException("GMail OAuth2 configuration is missing.");
            }

            var client = _httpClientFactory.CreateClient();
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _settings.GmailClientId,
                ["client_secret"] = _settings.GmailClientSecret,
                ["refresh_token"] = _settings.GmailRefreshToken,
                ["grant_type"] = "refresh_token"
            });

            var resp = await client.PostAsync(GmailTokenEndpoint, content, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh access token. Status: {Status}, Body: {Body}", resp.StatusCode, body);
                throw new InvalidOperationException("Failed to refresh Gmail access token. See logs for details.");
            }

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("access_token", out var tok))
            {
                var accessToken = tok.GetString();
                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Token response missing access_token: {Body}", body);
                    throw new InvalidOperationException("No access_token in token response.");
                }
                return accessToken;
            }

            _logger.LogError("No access_token in response: {Body}", body);
            throw new InvalidOperationException("No access_token in token response.");
        }

        // Helper: encode raw MIME to base64url string (no padding)
        private static string Base64UrlEncode(byte[] bytes)
        {
            var s = Convert.ToBase64String(bytes);
            s = s.Split('=')[0]; // Remove trailing '='
            s = s.Replace('+', '-').Replace('/', '_');
            return s;
        }

        // Build MIME message (no sending here)
        private MimeMessage BuildMessage(
            List<string> toEmails,
            string subject,
            string htmlContent,
            List<string>? ccEmails = null,
            List<string>? bccEmails = null,
            IEnumerable<(byte[] bytes, string fileName, string mimeType)>? attachments = null)
        {
            var validTo = toEmails.Where(IsValidEmail).Distinct().ToList();
            var validCc = ccEmails?.Where(IsValidEmail).Distinct().ToList() ?? new List<string>();
            var validBcc = bccEmails?.Where(IsValidEmail).Distinct().ToList() ?? new List<string>();

            if (!validTo.Any())
                throw new ArgumentException("At least one valid recipient is required.", nameof(toEmails));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            foreach (var to in validTo) message.To.Add(MailboxAddress.Parse(to));
            foreach (var cc in validCc) message.Cc.Add(MailboxAddress.Parse(cc));
            foreach (var bcc in validBcc) message.Bcc.Add(MailboxAddress.Parse(bcc));
            message.Subject = subject ?? "";

            var builder = new BodyBuilder();

            // If attachments present, set HtmlBody and add attachments
            builder.HtmlBody = htmlContent ?? "";

            if (attachments != null)
            {
                foreach (var att in attachments)
                {
                    if (att.bytes == null || att.bytes.Length == 0) continue;
                    // If mimeType present, pass it; otherwise let MimeKit guess
                    if (!string.IsNullOrEmpty(att.mimeType))
                        builder.Attachments.Add(att.fileName, att.bytes, ContentType.Parse(att.mimeType));
                    else
                        builder.Attachments.Add(att.fileName, att.bytes);
                }
            }

            message.Body = builder.ToMessageBody();
            return message;
        }

        // Core send via Gmail REST API (raw MIME)
        private async Task<JsonDocument> SendRawMimeAsync(MimeMessage mimeMessage, CancellationToken cancellationToken = default)
        {
            // Serialize MIME
            using var ms = new MemoryStream();
            mimeMessage.WriteTo(ms);
            var rawBytes = ms.ToArray();
            var rawBase64Url = Base64UrlEncode(rawBytes);

            var payload = new { raw = rawBase64Url };

            var http = _httpClientFactory.CreateClient();
            var accessToken = await GetAccessTokenAsync(cancellationToken);

            using var request = new HttpRequestMessage(HttpMethod.Post, GmailSendEndpoint);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await http.SendAsync(request, cancellationToken);
            var respBody = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Gmail send failed. Status: {Status}. Body: {Body}", resp.StatusCode, respBody);
                throw new InvalidOperationException($"Gmail send failed: {resp.StatusCode} - {respBody}");
            }

            // parse response JSON
            var doc = JsonDocument.Parse(respBody);
            _logger.LogInformation("Gmail send response: {Body}", respBody);
            return doc;
        }

        // Public: send without attachment
        public async Task SendEmailAsync(
            List<string> toEmails,
            string subject,
            string htmlContent,
            List<string>? ccEmails = null,
            List<string>? bccEmails = null,
            CancellationToken cancellationToken = default)
        {
            var mime = BuildMessage(toEmails, subject, htmlContent, ccEmails, bccEmails, attachments: null);
            var doc = await SendRawMimeAsync(mime, cancellationToken);

            // optional: inspect labelIds in response
            if (doc.RootElement.TryGetProperty("labelIds", out var labelIds))
            {
                _logger.LogInformation("Message labelIds: {Labels}", labelIds.ToString());
            }
        }

        // Public: send with single attachment (bytes)
        public async Task SendEmailWithAttachmentAsync(
            List<string> toEmails,
            string subject,
            string htmlContent,
            byte[] fileBytes,
            string attachmentFileName,
            string? mimeType = null,
            List<string>? ccEmails = null,
            List<string>? bccEmails = null,
            CancellationToken cancellationToken = default)
        {
            var attachments = new List<(byte[] bytes, string fileName, string mimeType)> {
                (fileBytes, attachmentFileName, mimeType ?? "application/octet-stream")
            };

            var mime = BuildMessage(toEmails, subject, htmlContent, ccEmails, bccEmails, attachments);
            var doc = await SendRawMimeAsync(mime, cancellationToken);

            if (doc.RootElement.TryGetProperty("labelIds", out var labelIds))
            {
                _logger.LogInformation("Message labelIds: {Labels}", labelIds.ToString());
            }
        }
    }
}
