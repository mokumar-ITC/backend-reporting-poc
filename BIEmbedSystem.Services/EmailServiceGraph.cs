using Azure.Identity;
using BIEmbedSystem.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace BIEmbedSystem.Services
{
    public class EmailServiceGraph
    {
        private readonly GraphServiceClient _graphClient;
        private readonly AadService aadService;
        private readonly AzureAdSettings _azureAd;

        public EmailServiceGraph(AadService aadService, IOptions<AzureAdSettings> azureAd)
        {
            this.aadService = aadService;
            _azureAd = azureAd.Value;

            var clientSecretCredential = new ClientSecretCredential(
                _azureAd.TenantId,
                _azureAd.ClientId,
                _azureAd.ClientSecret
            );

            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        public async Task SendEmailAsync(string fromEmail, string toEmail, string subject, string bodyHtml)
        {
            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = bodyHtml
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = toEmail
                        }
                    }
                }
            };


            await _graphClient.Users[fromEmail]
                .SendMail
                .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });
        }
    }
}
