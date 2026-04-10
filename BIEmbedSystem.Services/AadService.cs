// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace BIEmbedSystem.Services
{
    using BIEmbedSystem.Core.Entities;
    using Microsoft.Extensions.Options;
    using Microsoft.Identity.Client;
    using Microsoft.PowerBI.Api;
    using Microsoft.PowerBI.Api.Models;
    using Microsoft.Rest;
    using System;
    using System.Linq;
    using System.Security;
    using System.Threading.Tasks;

    public class AadService
    {
        private readonly IOptions<AzureAdSettings> azureAd;

        public AadService(IOptions<AzureAdSettings> azureAd)  
        {
            this.azureAd = azureAd;
        }

        /// <summary>
        /// Generates and returns Access token
        /// </summary>
        /// <returns>AAD token</returns>
        public async Task<string> GetAccessToken()
        {
            AuthenticationResult authenticationResult = null;
            if (azureAd.Value.AuthenticationMode.Equals("masteruser", StringComparison.InvariantCultureIgnoreCase))
            {
                // Create a public client to authorize the app with the AAD app
                IPublicClientApplication clientApp = PublicClientApplicationBuilder.Create(azureAd.Value.ClientId).WithAuthority(azureAd.Value.AuthorityUrl).Build();
                var userAccounts = await clientApp.GetAccountsAsync();

                try
                {

                    // Retrieve Access token from cache if available
                    authenticationResult =  await clientApp.AcquireTokenSilent(azureAd.Value.ScopeBase, userAccounts.FirstOrDefault()).ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    SecureString password = new SecureString();
                    foreach (var key in azureAd.Value.PbiPassword)
                    {
                        password.AppendChar(key);
                    }
                    authenticationResult = await clientApp.AcquireTokenByUsernamePassword(azureAd.Value.ScopeBase, azureAd.Value.PbiUsername, password).ExecuteAsync();
                }
            }

            // Service Principal auth is the recommended by Microsoft to achieve App Owns Data Power BI embedding
            else if (azureAd.Value.AuthenticationMode.Equals("serviceprincipalActive", StringComparison.InvariantCultureIgnoreCase))
            {
                // For app only authentication, we need the specific tenant id in the authority url
                var tenantSpecificUrl = azureAd.Value.AuthorityUrl.Replace("organizations", azureAd.Value.TenantId);

                // Create a confidential client to authorize the app with the AAD app
                IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                                                                .Create(azureAd.Value.ClientId)
                                                                                .WithClientSecret(azureAd.Value.ClientSecret)
                                                                                .WithAuthority(tenantSpecificUrl)
                                                                                .Build();
                // Make a client call if Access token is not available in cache
                authenticationResult = await clientApp.AcquireTokenForClient(azureAd.Value.ScopeBase).ExecuteAsync();
            } // Service Principal auth is the recommended by Microsoft to achieve App Owns Data Power BI embedding
           
            else if (azureAd.Value.AuthenticationMode.Equals("serviceprincipal", StringComparison.InvariantCultureIgnoreCase))
            {
                

                // For app only authentication, we need the specific tenant id in the authority url
                var tenantSpecificUrl = azureAd.Value.AuthorityUrl.Replace("organizations", azureAd.Value.TenantId);

                // Create a confidential client to authorize the app with the AAD app
                IConfidentialClientApplication clientApp = ConfidentialClientApplicationBuilder
                                                                                .Create(azureAd.Value.ClientId)
                                                                                .WithClientSecret(azureAd.Value.ClientSecret)
                                                                                .WithAuthority(tenantSpecificUrl)
                                                                                .Build();
                // Make a client call if Access token is not available in cache
                authenticationResult = await clientApp.AcquireTokenForClient(azureAd.Value.ScopeBase).ExecuteAsync();
            }


            return authenticationResult.AccessToken;
        }
        public async Task<string> GetEmbedTokenAsync()
        {
            try {
                // 1) Acquire an app-only token (service principal) for the Power BI resource
            var authority = $" https://login.microsoftonline.com/{azureAd.Value.TenantId}";
            //var authority=$"https://login.microsoftonline.com/common/";
            var pbiScope = new[] { " https://analysis.windows.net/powerbi/api/.default " };

            var app = ConfidentialClientApplicationBuilder
                .Create(azureAd.Value.ClientId)
                .WithClientSecret(azureAd.Value.ClientSecret)
                .WithAuthority(authority)
                .Build();

            var authResult = await app.AcquireTokenForClient(pbiScope).ExecuteAsync();
                return authResult.AccessToken;
            }
            catch (Exception ex) {
                return ex.Message;
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                // Power BI Scope
                string[] scopes = { "https://analysis.windows.net/powerbi/api/.default" };

                // Authority (tenant-specific is recommended)
                string authority = $"https://login.microsoftonline.com/{azureAd.Value.TenantId}";

                // Build confidential client (service principal)
                var app = ConfidentialClientApplicationBuilder.Create(azureAd.Value.ClientId)
                    .WithClientSecret(azureAd.Value.ClientSecret)
                    .WithAuthority(authority)
                    .Build();

                // Acquire token
                var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

                // Return ONLY the Azure AD access token (JWT)
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }
}
