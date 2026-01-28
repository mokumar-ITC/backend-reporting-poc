using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Dapper; // optional - using Dapper for quick SQL lookup
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

public class PowerBiService : IPowerBiService
{
    private readonly AadService aadService;
    private readonly IOptions<AzureAdSettings> azureAd;
    private readonly string PowerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
    private readonly string powerBiApiUrl = "https://api.powerbi.com";
    private readonly MDMDbContext _db;
    private readonly HttpClient _http;


    public PowerBiService(AadService aadService, IOptions<AzureAdSettings> azureAd, MDMDbContext db, HttpClient http)
    {
        this.aadService = aadService;
        this.azureAd = azureAd;
        _db = db;
        _http = http;
    }

    // 1) Get AAD access token for Power BI using client credentials
    public async Task<PowerBIClient> GetPowerBIClient()
    {
        var accessToken = await aadService.GetAccessToken();
        var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
        return new PowerBIClient(new Uri(powerBiApiUrl), tokenCredentials);
    }

    // 2) Check security table for the user's existence and optionally get Asset_Id
    private async Task<string?> GetAssetIdForUserAsync(string userEmail)
    {
        // normalize email
        var normalized = userEmail?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(normalized)) return null;

        var getAssetId = await _db.PowerBI_Security.Where(u => u.Email == userEmail).FirstOrDefaultAsync();

        var assetId = getAssetId.AssetId;
        return assetId;
    }

    // 3) Generate embed token
    public async Task<EmbedResponseDto> GenerateEmbedTokenAsync(EmbedRequestDto request)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));
        var user = request.UserEmail?.Trim();

        // look up the Asset_ID — only to decide whether the user exists; DAX will compare asset_id internally
        var assetId = await GetAssetIdForUserAsync(user);

        var aadToken = await aadService.GetAccessToken();

        // Build request body for GenerateToken endpoint for a report in a group
        // We pass identities array so USERPRINCIPALNAME() in DAX will equal request.UserEmail
        var generateTokenUrl = $"{powerBiApiUrl}/v1.0/myorg/groups/{request.WorkspaceId}/reports/{request.ReportId}/GenerateToken";

        // identities payload: username must match the dataset's expected principal (your DAX checks USERPRINCIPALNAME()).
        // role must be set if the dataset requires role — otherwise leave roles empty array.
        var identities = new List<object>();

        if (!string.IsNullOrEmpty(user))
        {
            identities.Add(new
            {
                username = user,
                datasets = new[] { request.DatasetId }  // <-- REQUIRED FOR RLS
                                                        // roles = new string[] { "RLSRoleName" }  // optional
            });
        }

        var body = new
        {
            datasets = new[]
        {
            new { id = request.DatasetId }
        },
                reports = new[]
        {
            new { id = request.ReportId, groupId = request.WorkspaceId }
        },
                targetWorkspaces = new[]
        {
            new { id = request.WorkspaceId }
        },
            identities = identities   // your existing RLS identities
        };

        var json = JsonConvert.SerializeObject(body);

        var httpReq = new HttpRequestMessage(HttpMethod.Post, "https://api.powerbi.com/v1.0/myorg/GenerateToken")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aadToken);

        var resp = await _http.SendAsync(httpReq);

        var content = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            // helpful debug - bubble up meaningful error
            throw new Exception($"GenerateToken failed: {resp.StatusCode} - {content}");
        }

        dynamic tokenResult = JsonConvert.DeserializeObject(content);

        // Build embed url: get report's embedUrl (report resource)
        // We need to fetch report metadata to get embedUrl or you may already know it
        var reportMetaUrl = $"{powerBiApiUrl}/v1.0/myorg/groups/{request.WorkspaceId}/reports/{request.ReportId}";
        var metaReq = new HttpRequestMessage(HttpMethod.Get, reportMetaUrl);
        var metaRes = await _http.SendAsync(metaReq);
        metaRes.EnsureSuccessStatusCode();
        var metaJson = await metaRes.Content.ReadAsStringAsync();
        dynamic meta = JsonConvert.DeserializeObject(metaJson);
        string embedUrl = meta.embedUrl;

        return new EmbedResponseDto
        {
            EmbedToken = (string)tokenResult.token,
            ExpiresOn = DateTime.UtcNow.AddSeconds((int?)tokenResult.expirationSeconds ?? 3600),
            EmbedUrl = embedUrl,
            ReportId = request.ReportId
        };
    }

    
}
