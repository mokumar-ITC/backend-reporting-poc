using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;

namespace YourNamespace.Services
{
    public class FabricCapacityServiceWithVersion
    {
        private readonly HttpClient _httpClient;
        private readonly TokenCredential _credential;
        private readonly string _apiVersion;

        public FabricCapacityServiceWithVersion(string apiVersion = "2023-11-01")
        {
            _httpClient = new HttpClient();
            _credential = new DefaultAzureCredential();
            _apiVersion = apiVersion;
        }

        /// <summary>
        /// Get a Fabric Capacity resource using REST API (bypassing the SDK)
        /// </summary>
        public async Task<JsonDocument?> GetCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            if (string.IsNullOrEmpty(subscriptionId) ||
                string.IsNullOrEmpty(resourceGroupName) ||
                string.IsNullOrEmpty(capacityName))
            {
                throw new ArgumentException("Subscription ID, resource group name, and capacity name are required.");
            }

            string requestUri =
                $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}?api-version={_apiVersion}";

            // Acquire token using Azure.Identity
            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }), CancellationToken.None);

            // Add Authorization header
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Token);

            // Make the GET call
            HttpResponseMessage response = await _httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error fetching Fabric Capacity: {response.StatusCode}\n{error}");
            }

            string jsonContent = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(jsonContent);
        }

        /// <summary>
        /// Example: Resume or pause Fabric Capacity
        /// </summary>
        public async Task<bool> ResumeCapacityAsync(string subscriptionId, string resourceGroupName, string capacityName)
        {
            string requestUri =
                $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Fabric/capacities/{capacityName}/resume?api-version={_apiVersion}";

            AccessToken token = await _credential.GetTokenAsync(
                new TokenRequestContext(new[] { "https://management.azure.com/.default" }), CancellationToken.None);

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.Token);

            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, null);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Error resuming Fabric Capacity: {response.StatusCode}\n{error}");
            }

            return true;
        }
    }
}
