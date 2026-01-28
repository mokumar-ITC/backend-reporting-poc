using Azure.Identity;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.PowerBI.Api.Models;
using Role = BIEmbedSystem.Core.Entities.Role;

namespace BIEmbedSystem.Services
{
    public class AzureGraphService
    {
        private readonly AadService aadService;
        private readonly IOptions<AzureAdSettings> azureAd;
        private readonly string powerBiApiUrl = "https://api.powerbi.com";
        private readonly MDMDbContext _db;

        public AzureGraphService(AadService aadService, IOptions<AzureAdSettings> azureAd, MDMDbContext db)
        {
            this.aadService = aadService;
            this.azureAd = azureAd;
            _db = db;
        }

        public async Task<dynamic> GetUserList()
        {
            //var authority = $"https://login.microsoftonline.com/{tenantId}";
            //List<Users> list = new List<Users>();


            // Build credential
            var credential = new ClientSecretCredential(
                azureAd.Value.TenantId,
                azureAd.Value.ClientId,
                azureAd.Value.ClientSecret
            );
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // Authenticate with ClientSecretCredential
            var clientSecretCredential = new ClientSecretCredential(
                azureAd.Value.TenantId,
                 azureAd.Value.ClientId,
                azureAd.Value.ClientSecret,
                options);


          //  var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
          //  var graphClient = new GraphServiceClient(credential, " https://graph.microsoft.com/.default ");
            // Create Graph client (with ClientCredentialProvider)
            //var graphClient = new GraphServiceClient(clientSecretCredential);

            //// Get users with specific properties including email
            //var users = await graphClient.Users
            //    .Request()
            //    .Select(u => new { u.DisplayName, u.Mail, u.UserPrincipalName }) // Specify properties to retrieve
            //    .GetAsync();

            //// foreach (var a in users.Value)
            //foreach (var user in users.CurrentPage)
            //{
            //    if (a.PrincipalType == "User")
            //    {
            //        // Access user email (use u.Mail or u.UserPrincipalName)
            //        var userEmail = user.Mail ?? user.UserPrincipalName;
            //        Console.WriteLine($"User: {user.DisplayName}, Email: {userEmail}");
            //    }
            //}
            // Create GraphServiceClient
            var graphClient1 = new GraphServiceClient(clientSecretCredential);

            // Call Graph API to get users
            //var users1 = await graphClient1.Users.GetAsync();

            // Create Graph client (no more ClientCredentialProvider)
            var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

            //var users = await graphClient.Users.GetAsync();
            var assignments = await graphClient.ServicePrincipals[azureAd.Value.ObjectId]
                .AppRoleAssignedTo
                .GetAsync();
           List< AzureUser> azureUsers = new List<AzureUser>();

            foreach (var a in assignments.Value)
            {
                AzureUser azure    = new AzureUser();
                if (a.PrincipalType == "User")
                {
                    var user = await graphClient.Users[a.PrincipalId.ToString()]
                        .GetAsync(config =>
                        {
                            config.QueryParameters.Select = new[] { "id", "displayName", "userPrincipalName", "mail" };
                        });
                    
                    Console.WriteLine($"User: {user.DisplayName}, Email: {user.Mail ?? user.UserPrincipalName}");
                    azure.Email = user.Mail;
                    azure.UserName = user.DisplayName;
                    azure.Role = user.UserPrincipalName;
                    if (!azureUsers.Any(u => string.Equals(u.Email, azure.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        azureUsers.Add(azure);
                    }
                    //return user;
                }
                else if (a.PrincipalType == "Group")
                {
                    Console.WriteLine($"Group: {a.PrincipalDisplayName}");
                }
                else
                {
                    Console.WriteLine($"{a.PrincipalType}: {a.PrincipalDisplayName}");
                }
            }


            return azureUsers;
        }
        public async Task<dynamic> GetRolesList()
        {

            // Build credential
            var credential = new ClientSecretCredential(
                azureAd.Value.TenantId,
                azureAd.Value.ClientId,
                azureAd.Value.ClientSecret
            );

            // Create Graph client (no more ClientCredentialProvider)
            var graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

            var appInfo = await graphClient.ServicePrincipals[azureAd.Value.ObjectId]
                .GetAsync();
            var roles = appInfo.AppRoles;
            return roles;
        }
        public async Task<List<Role>> GetRolesByOrganisationAsync(int organisationId)
        {
            // Check if the organisationId exists in table
            bool exists = await _db.Roles.AnyAsync(r => r.OrganizationId == organisationId);

            if (!exists)
            {
                return await _db.Roles
                .Where(r => r.OrganizationId == 0)
                .ToListAsync();
            }

            return await _db.Roles
                .Where(r => r.OrganizationId == organisationId || r.OrganizationId == 0)
                .ToListAsync();
        }

    }
}
