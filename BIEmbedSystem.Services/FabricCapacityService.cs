using Azure; // Required for RequestFailedException, WaitUntil
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Fabric;
using Azure.ResourceManager.Fabric.Models;
using Azure.ResourceManager.Resources;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Core.Interfaces;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.PowerBI.Api.Models;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace BIEmbedSystem.Services
{
    public class FabricCapacityService
    {
        private readonly ILogger<FabricCapacityService> _logger;
        private readonly ArmClient _armClient;
        private readonly AzureAdSettings _azureAd;
        private readonly MDMDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _config;

        public FabricCapacityService(ILogger<FabricCapacityService> logger, IOptions<AzureAdSettings> azureAdOptions, MDMDbContext context, EmailService emailService, IConfiguration config)
        {
            _logger = logger;
            _azureAd = azureAdOptions.Value;
            var tenantId = _azureAd.TenantId;
            var clientId = _azureAd.ClientId;
            var clientSecret = _azureAd.ClientSecret;
            _context = context;
            // Create the credential
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            _armClient = new ArmClient(credential);
            _emailService = emailService;
            _config = config;

        }


        // --- Helper Methods ---

        private async Task<ResourceGroupResource> GetResourceGroupAsync(string subscriptionId, string resourceGroupName)
        {
            ResourceIdentifier rgId = ResourceGroupResource.CreateResourceIdentifier(subscriptionId, resourceGroupName);
            ResourceGroupResource resourceGroup = _armClient.GetResourceGroupResource(rgId);
            return await resourceGroup.GetAsync();
        }

        //private async Task<SubscriptionResource> GetSubscriptionAsync(string subscriptionId)
        //{
        //    ResourceIdentifier subId = new ResourceIdentifier($"/subscriptions/{subscriptionId}");
        //    return await _armClient.GetSubscriptionResource(subId).GetAsync();
        //}

        public async Task<JsonDocument?> GetSubscriptionAsync(string subscriptionId)
        {
            try
            {
                // Define API version and full request URL
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Fabric?api-version={apiVersion}";

                // Authenticate using Azure AD App credentials
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // Configure HTTP client
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                var response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    var json = await JsonDocument.ParseAsync(responseStream);
                    _logger.LogInformation("Successfully fetched Fabric Capacity details for: {SubCription}", subscriptionId);
                    return json;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to fetch Fabric Capacity details. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Fabric Capacity details for {subscriptionId}.", subscriptionId);
                throw;
            }
        }

        public async Task<JsonDocument?> GetCapacityResourceAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            try
            {
                // Define API version and full request URL
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={apiVersion}";

                // Authenticate using Azure AD App credentials
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // Configure HTTP client
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                _logger.LogInformation("Fetching Fabric Capacity details for: {CapacityName}", capacityName);

                // Execute GET request
                var response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();
                    var json = await JsonDocument.ParseAsync(responseStream);

                    _logger.LogInformation("Successfully fetched Fabric Capacity details for: {CapacityName}", capacityName);
                    return json;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to fetch Fabric Capacity details. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Fabric Capacity details for {CapacityName}.", capacityName);
                throw;
            }
        }

        public async Task<FabricCapacityDto?> GetCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            try
            {
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={apiVersion}";

                // Authenticate via Azure AD App credentials
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Fetching Fabric Capacity details for: {CapacityName}", capacityName);
                var response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // Deserialize into your lightweight DTO (safe, no constructor mismatch)
                    //var result = JsonSerializer.Deserialize<FabricCapacityDto>(
                    //    json,
                    //    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    //);
                    var result = JsonSerializer.Deserialize<FabricCapacityDto>(
                        json,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            ReadCommentHandling = JsonCommentHandling.Skip,
                            AllowTrailingCommas = true,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                        }
                    );


                    _logger.LogInformation("Successfully fetched Fabric Capacity: {CapacityName}", capacityName);
                    return result;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Fabric Capacity not found: {CapacityName}", capacityName);
                    return null;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to get Fabric Capacity. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Fabric Capacity details for {CapacityName}.", capacityName);
                throw;
            }
        }


        // --- CRUD & LIST Operations ---

        // 1. GET (Capacities - Get)
        // API: GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}
        //public async Task<FabricCapacityData?> GetCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        //{
        //    try
        //    {
        //        var capacityResource = await GetCapacityResourceAsync(subscriptionId, resourceGroupName, capacityName);
        //        return capacityResource?.Data;
        //    }
        //    catch (RequestFailedException ex) when (ex.Status == 404)
        //    {
        //        return null;
        //    }
        //}

        // 2. CREATE or UPDATE (Capacities - Create Or Update)
        // API: PUT /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}
        public async Task<FabricCapacityDto?> CreateOrUpdateCapacityAsync(
        string subscriptionId,
        string resourceGroupName,
        string capacityName,
        FabricCapacityCreationData data)
        {
            try
            {
                // --- Step 1: Build API endpoint ---
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={apiVersion}";

                // --- Step 2: Build PUT request body ---
                // As per Azure REST spec: https://learn.microsoft.com/en-us/rest/api/microsoftfabric/fabric-capacities/create-or-update
                var requestBody = new
                {
                    location = data.Location, // Required field
                    sku = new
                    {
                        name = data.SkuName,    // e.g., "F2"
                        tier = "Fabric"         // Fixed value
                    },
                    properties = new
                    {
                        administration = new
                        {
                            members = data.Admins // List<string>
                        }
                    },
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // --- Step 3: Get Azure access token ---
                var credential = new ClientSecretCredential(
                    _azureAd.TenantId,
                    _azureAd.ClientId,
                    _azureAd.ClientSecret
                );

                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Step 4: Configure HttpClient ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json")
                );

                // --- Step 5: Send PUT request (for create or update) ---
                var response = await httpClient.PutAsync(requestUri, jsonContent);

                string responseText = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CreateOrUpdateCapacity response: {ResponseText}", responseText);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FabricCapacityDto>(
                        responseText,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    _logger.LogInformation("✅ Successfully created/updated Fabric Capacity: {CapacityName}", capacityName);
                    return result;
                }
                else
                {
                    _logger.LogError("❌ Failed to create/update Fabric Capacity. Status: {Status}, Response: {Error}",
                        response.StatusCode, responseText);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating or updating Fabric Capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        // 3. UPDATE (Capacities - Update)
        // API: PATCH /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}
        public async Task<FabricCapacityDto?> UpdateCapacityAsync(
        string subscriptionId,
        string resourceGroupName,
        string capacityName,
        FabricCapacityPatchData data)
        {
            try
            {
                // --- Step 1: Build API endpoint ---
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={apiVersion}";

                // --- Step 2: Build PATCH payload ---
                // This matches the ARM schema for updating capacity properties.
                var patchPayload = new Dictionary<string, object>();

                if (data.SkuName != null)
                {
                    patchPayload["sku"] = new
                    {
                        name = data.SkuName,
                        tier = "Fabric"
                    };
                }

                if (data.Admins != null && data.Admins.Any())
                {
                    patchPayload["properties"] = new
                    {
                        administration = new
                        {
                            members = data.Admins
                        }
                    };
                }

                if (data.Tags != null && data.Tags.Count > 0)
                {
                    patchPayload["tags"] = data.Tags;
                }

                // Serialize to JSON
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(patchPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // --- Step 3: Get token from Azure AD App Credentials ---
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Step 4: Configure HttpClient ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // --- Step 5: Send PATCH request ---
                var method = new HttpMethod("PATCH");
                var request = new HttpRequestMessage(method, requestUri) { Content = jsonContent };

                _logger.LogInformation("Updating Fabric Capacity {CapacityName}...", capacityName);
                var response = await httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    using var responseStream = await response.Content.ReadAsStreamAsync();

                    // Deserialize into FabricCapacityData (your SDK model)
                    var updatedCapacity = await JsonSerializer.DeserializeAsync<FabricCapacityDto>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    //send Email to adminstrator member for notification
                    string subject = $"⚙️ Capacity '{capacityName}' Status Update";
                    string body = $@"
                        <h3>Fabric Capacity Status Changed</h3>
                        <p>The capacity <strong>{capacityName}</strong> has Updated its SKU to:</p>
                        <h2 style='color:#2563EB'>{data.SkuName}</h2>
                        <p>Time: {DateTime.Now:f}</p>
                        <hr />
                        <p>This is an automated notification from <strong>Reporting Hub</strong>.</p>";

                    var getCapacity = await GetCapacityAsync(subscriptionId, resourceGroupName, capacityName);
                    await _emailService.SendEmailAsync(getCapacity.Properties.Administration.Members, subject, body);

                    _logger.LogInformation("Successfully updated Fabric Capacity: {CapacityName}", capacityName);
                    return updatedCapacity;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update Fabric Capacity. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Fabric Capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        // 4. DELETE (Capacities - Delete)
        // API: DELETE /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}
        public async Task<bool> DeleteCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            try
            {
                // --- Step 1: Build request URL ---
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={apiVersion}";

                // --- Step 2: Get Bearer Token using ClientSecretCredential ---
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Step 3: Configure HttpClient with Authorization header ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                _logger.LogInformation("Deleting Fabric Capacity {CapacityName}...", capacityName);

                // --- Step 4: Perform DELETE Request ---
                var response = await httpClient.DeleteAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully deleted Fabric Capacity: {CapacityName}", capacityName);
                    return true;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Fabric Capacity {CapacityName} not found, nothing to delete.", capacityName);
                    return false;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to delete Fabric Capacity. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Fabric Capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        // 5. LIST BY RESOURCE GROUP (Capacities - List By Resource Group)
        // API: GET /subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities
        public async Task<IEnumerable<FabricCapacityData>> ListCapacitiesByResourceGroupAsync(string subscriptionId, string resourceGroupName)
        {
            ResourceGroupResource resourceGroup = await GetResourceGroupAsync(subscriptionId, resourceGroupName);
            FabricCapacityCollection capacityCollection = resourceGroup.GetFabricCapacities();

            var capacities = new List<FabricCapacityResource>();
            await foreach (var c in capacityCollection.GetAllAsync())
            {
                capacities.Add(c);
            }
            return capacities.Select(c => c.Data);
        }

        // 6. LIST BY SUBSCRIPTION (Capacities - List By Subscription)
        // API: GET /subscriptions/{subscriptionId}/providers/Microsoft.Fabric/capacities
        public async Task<IEnumerable<FabricCapacityDto>> ListCapacitiesBySubscriptionAsync(string subscriptionId)
        {
            try
            {
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Fabric/capacities?api-version={apiVersion}";

                // --- Get Bearer Token using ClientSecretCredential ---
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Configure HttpClient ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation("Fetching list of Fabric Capacities for Subscription {SubscriptionId}...", subscriptionId);

                // --- Perform GET request ---
                var response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();

                    // --- Deserialize JSON into wrapper type ---
                    var result = JsonSerializer.Deserialize<FabricCapacityListResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result?.Value != null)
                    {
                        _logger.LogInformation("Retrieved {Count} Fabric Capacities for Subscription {SubscriptionId}.",
                            result.Value.Count, subscriptionId);
                        return result.Value;
                    }

                    _logger.LogWarning("No Fabric Capacities found for Subscription {SubscriptionId}.", subscriptionId);
                    return Enumerable.Empty<FabricCapacityDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to list Fabric Capacities. Status: {StatusCode}, Response: {Error}",
                        response.StatusCode, error);
                    return Enumerable.Empty<FabricCapacityDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Fabric Capacities for Subscription {SubscriptionId}.", subscriptionId);
                throw;
            }
        }



        // --- SKU & LIFECYCLE Operations ---

        // 7. LIST SKUs (Operations - List Skus)
        // API: GET /providers/Microsoft.Fabric/skus?api-version=2023-11-01
        public async Task<IEnumerable<FabricSkuDetailsForNewCapacity>> ListSkusAsync()
        {
            SubscriptionResource subscription = await _armClient.GetDefaultSubscriptionAsync();
            var skus = new List<FabricSkuDetailsForNewCapacity>();
            await foreach (var s in subscription.GetSkusFabricCapacitiesAsync())
            {
                skus.Add(s); // FIX: Use 's' directly, not 's.DetailsForNewCapacity'
            }
            return skus;
        }

        // 8. LIST SKUs FOR CAPACITY (Operations - List Skus For Capacity)
        // API: POST /subscriptions/{subId}/resourceGroups/{rgName}/providers/Microsoft.Fabric/capacities/{capacityName}/listSkus
        public async Task<IEnumerable<FabricSkuDto>> ListSkusForCapacityAsync(
        string subscriptionId,
        string resourceGroupName,
        string capacityName)
        {
            try
            {
                // --- Step 1: Build request URL ---
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}/skus?api-version={apiVersion}";

                // --- Step 2: Get Bearer Token ---
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Step 3: Configure HttpClient ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation(
                    "Fetching SKUs for Fabric Capacity {CapacityName} in Resource Group {ResourceGroup}...",
                    capacityName, resourceGroupName);

                // --- Step 4: Perform GET request ---
                var response = await httpClient.GetAsync(requestUri);

                if (response.IsSuccessStatusCode)
                {

                    var json = await response.Content.ReadAsStringAsync();

                    // --- Deserialize JSON into wrapper type ---
                    var result = JsonSerializer.Deserialize<FabricSkuListResponse>(
                        json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );

                    if (result != null)
                    {
                        _logger.LogInformation("Retrieved {Count} Fabric Capacities for Subscription {SubscriptionId}.",
                            result.Value.Count, subscriptionId);
                        //return result.Value;
                        // ✅ Flatten the nested list
                        var skuList = result?.Value?
                            .Where(v => v.Sku != null)
                            .Select(v => v.Sku!)
                            .ToList() ?? new List<FabricSkuDto>();

                        return skuList; // This is IEnumerable<FabricSkuDto>
                    }

                    _logger.LogWarning("No Fabric Capacities found for Subscription {SubscriptionId}.", subscriptionId);
                    return Enumerable.Empty<FabricSkuDto>();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to fetch SKUs for Fabric Capacity {CapacityName}. Status: {StatusCode}, Response: {Error}",
                        capacityName, response.StatusCode, error);
                    return Enumerable.Empty<FabricSkuDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching SKUs for Fabric Capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        public async Task<bool> ResumeCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            try
            {
                // Define API version and endpoint
                string apiVersion = "2023-11-01";
                string requestUri = $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}/resume?api-version={apiVersion}";

                // Use Azure AD App credentials to get token
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // Prepare HTTP client with authorization header
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                // POST call — resume has no request body, so send empty JSON or null
                var jsonContent = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

                _logger.LogInformation("Resuming Fabric capacity: {CapacityName}", capacityName);
                var response = await httpClient.PostAsync(requestUri, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    //send Email to adminstrator member for notification
                    string subject = $"⚙️ Capacity '{capacityName}' Status Update";
                    string body = $@"
                        <h3>Fabric Capacity Status Changed</h3>
                        <p>The capacity <strong>{capacityName}</strong> has changed its status to:</p>
                        <h2 style='color:#2563EB'>Resumed</h2>
                        <p>Time: {DateTime.Now:f}</p>
                        <hr />
                        <p>This is an automated notification from <strong>Reporting Hub</strong>.</p>";

                    var getCapacity = await GetCapacityAsync(subscriptionId, resourceGroupName, capacityName);
                    await _emailService.SendEmailAsync(getCapacity.Properties.Administration.Members, subject, body);

                    _logger.LogInformation("Successfully resumed Fabric capacity {CapacityName}", capacityName);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to resume Fabric capacity. Status: {StatusCode}, Response: {Response}",
                        response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming Fabric capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        // 10. SUSPEND (Capacities - Suspend)
        // API: POST /subscriptions/{subId}/resourceGroups/{rgName}/providers/Microsoft.Fabric/capacities/{capacityName}/suspend
        public async Task<bool> SuspendCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            try
            {
                // --- Step 1: Build the request URL ---
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}/suspend?api-version={apiVersion}";

                // --- Step 2: Acquire Azure AD token using ClientSecretCredential ---
                var credential = new ClientSecretCredential(_azureAd.TenantId, _azureAd.ClientId, _azureAd.ClientSecret);
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                // --- Step 3: Configure HttpClient with Authorization header ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                _logger.LogInformation(
                    "Sending Suspend request for Fabric Capacity '{CapacityName}' in Resource Group '{ResourceGroupName}'.",
                    capacityName, resourceGroupName);

                // --- Step 4: POST request (body can be empty) ---
                var response = await httpClient.PostAsync(requestUri, null);

                // --- Step 5: Handle response ---
                if (response.IsSuccessStatusCode)
                {
                    //send Email to adminstrator member for notification
                    string subject = $"⚙️ Capacity '{capacityName}' Status Update";
                    string body = $@"
                        <h3>Fabric Capacity Status Changed</h3>
                        <p>The capacity <strong>{capacityName}</strong> has changed its status to:</p>
                        <h2 style='color:#2563EB'>Suspended</h2>
                        <p>Time: {DateTime.Now:f}</p>
                        <hr />
                        <p>This is an automated notification from <strong>Reporting Hub</strong>.</p>";
                    var getCapacity = await GetCapacityAsync(subscriptionId, resourceGroupName, capacityName);

                    await _emailService.SendEmailAsync(getCapacity.Properties.Administration.Members, subject, body);
                    _logger.LogInformation("Successfully resumed Fabric capacity {CapacityName}", capacityName);
                    return true;
                    _logger.LogInformation("Successfully suspended Fabric Capacity: {CapacityName}", capacityName);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to suspend Fabric Capacity {CapacityName}. Status: {StatusCode}, Response: {Error}",
                        capacityName, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending Fabric Capacity {CapacityName}.", capacityName);
                throw;
            }
        }


        // 3. POST Check Name Availability (Capacities - Check Name Availability)
        // API: POST https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Fabric/locations/{location}/checkNameAvailability?api-version=2023-11-01
        public async Task<FabricNameAvailabilityResultDto?> CheckNameAvailabilityAsync(string subscriptionId, string location, string name)
        {
            try
            {
                string apiVersion = "2023-11-01";
                string requestUri =
                    $"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Fabric/locations/{location}/checkNameAvailability?api-version={apiVersion}";

                // ✅ Body must have "name" and "type"
                var checkNameContent = new
                {
                    name = name,
                    type = "Microsoft.Fabric/capacities"
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(checkNameContent),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                // ✅ Get token for Azure Management API
                var credential = new ClientSecretCredential(
                    _azureAd.TenantId,
                    _azureAd.ClientId,
                    _azureAd.ClientSecret
                );

                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://management.azure.com/.default" }),
                    CancellationToken.None
                );

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

                var response = await httpClient.PostAsync(requestUri, jsonContent);

                string responseText = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("CheckNameAvailability response: {ResponseText}", responseText);

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<FabricNameAvailabilityResultDto>(
                        responseText,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    return result;
                }
                else
                {
                    _logger.LogError("CheckNameAvailability failed: {StatusCode} - {ResponseText}", response.StatusCode, responseText);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Fabric Capacity name availability.");
                throw;
            }
        }

        public async Task<CapacitySchedulerModel> CreateSchedulerAsync(CapacitySchedulerCreateRequest request)
        {
            var end_time = request.end_time ?? request.start_time.AddHours(request.DurationHours ?? 0);
            var entity = new CapacitySchedulerModel
            {
                CapacityName = request.CapacityName,
                start_time = request.start_time,
                end_time = end_time,
                last_run_time = request.last_run_time,
                Status = request.Status
            };

            _context.Capacity_Scheduler.Add(entity);
            await _context.SaveChangesAsync();

            return new CapacitySchedulerModel
            {
                Id = entity.Id,
                CapacityName = entity.CapacityName,
                start_time = entity.start_time,
                end_time = entity.end_time,
                last_run_time = entity.last_run_time,
                Status = entity.Status
            };
        }

        public async Task<List<CapacitySchedulerModel>> GetAllSchedulerAsync(string capacityName)
        {
            return await _context.Capacity_Scheduler
                .Where(x => x.CapacityName == capacityName)
                .Select(x => new CapacitySchedulerModel
                {
                    Id = x.Id,
                    CapacityName = x.CapacityName,
                    start_time = x.start_time,
                    end_time = x.end_time,
                    last_run_time = x.last_run_time,
                    Status = x.Status
                })
                .OrderBy(x => x.start_time) // optional: sort by time for better readability
                .ToListAsync();
        }


        public async Task<CapacitySchedulerModel?> GetSchedulerByIdAsync(int id) =>
            await _context.Capacity_Scheduler
                .Where(x => x.Id == id)
                .Select(x => new CapacitySchedulerModel
                {
                    Id = x.Id,
                    CapacityName = x.CapacityName,
                    start_time = x.start_time,
                    end_time = x.end_time,
                    last_run_time = x.last_run_time,
                    Status = x.Status
                })
                .FirstOrDefaultAsync();

        public async Task<CapacitySchedulerModel?> UpdateSchedulerAsync(int id, CapacitySchedulerUpdateRequest request)
        {
            var entity = await _context.Capacity_Scheduler.FindAsync(id);
            if (entity == null) return null;

            if (!string.IsNullOrEmpty(request.Status)) entity.Status = request.Status;
            if (request.start_time.HasValue) entity.start_time = request.start_time.Value;
            if (request.end_time.HasValue) entity.end_time = request.end_time.Value;
            if (request.duration.HasValue)
                entity.end_time = entity.start_time.AddHours(request.duration.Value);

            await _context.SaveChangesAsync();

            return await GetSchedulerByIdAsync(id);
        }

        public async Task<bool> DeleteSchedulerAsync(int id)
        {
            var entity = await _context.Capacity_Scheduler.FindAsync(id);
            if (entity == null) return false;

            _context.Capacity_Scheduler.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CapacitySchedulerModel>> GetActiveSchedulesAsync()
        {
            return await _context.Capacity_Scheduler
                .Where(s => s.Status == "Active")
                .ToListAsync();
        }
    }
    public class FabricSkuDto
    {
        public string Name { get; set; } = string.Empty;
        public string Tier { get; set; } = string.Empty;
    }

    public class FabricCapacityPropertiesDto
    {
        public string? Administration { get; set; }
        public string? ProvisioningState { get; set; }
        public string? State { get; set; }
    }
    public class FabricCapacityListResponse
    {
        public List<FabricCapacityDto>? Value { get; set; }
    }

    public class FabricSkuListResponse
    {
        public List<FabricSkuItem>? Value { get; set; }
    }

    public class FabricSkuItem
    {
        public FabricSkuDto? Sku { get; set; }
    }
    public class FabricCapacityDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Location { get; set; }

        public FabricSku? Sku { get; set; }
        public FabricCapacityProperties? Properties { get; set; }
    }

    public class FabricCapacityProperties
    {
        public FabricAdministration? Administration { get; set; }
        public string? ProvisioningState { get; set; }

        public string? State { get; set; }

    }

    public class FabricAdministration
    {
        public List<string>? Members { get; set; }
    }

    public class FabricSku
    {
        public string? Name { get; set; }
        public string? Tier { get; set; }
    }


    public class FabricNameAvailabilityResultDto
    {
        public bool NameAvailable { get; set; }
        public string? Reason { get; set; }
        public string? Message { get; set; }
    }


}