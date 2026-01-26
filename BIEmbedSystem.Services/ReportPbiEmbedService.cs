using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.TermStore;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.NativeInterop;
using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Group = Microsoft.PowerBI.Api.Models.Group;



namespace BIEmbedSystem.Services
{

    public class ReportPbiEmbedService
    {
        private readonly AadService aadService;
        private readonly IOptions<AzureAdSettings> azureAd;
        private readonly string PowerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
        private readonly string powerBiApiUrl = "https://api.powerbi.com";
        private readonly EmailService _emailService;
        private readonly MDMDbContext _db;

        public ReportPbiEmbedService(AadService aadService, IOptions<AzureAdSettings> azureAd, EmailService emailService, MDMDbContext db)
        {
            this.aadService = aadService;
            this.azureAd = azureAd;
            _emailService = emailService;
            _db = db;
        }
        private async Task<PowerBIClient> CreatePowerBIClientAsync()
        {
            // This is the common client creation logic for Admin APIs
            TokenCredential credential = new ClientSecretCredential(azureAd.Value.TenantId, azureAd.Value.ClientId, azureAd.Value.ClientSecret);
            AccessToken token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { PowerBIResourceUrl }),
                default // Add CancellationToken as the second argument
            );
            var credentials = new TokenCredentials(token.Token, "Bearer");
            return new PowerBIClient(new Uri("https://api.powerbi.com"), credentials);
        }

        /// <summary>
        /// Get Power BI client
        /// </summary>
        /// <returns>Power BI client object</returns>
        public async Task<PowerBIClient> GetPowerBIClient()
        {

            var accessToken = await aadService.GetAccessToken();
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
            return new PowerBIClient(new Uri(powerBiApiUrl), tokenCredentials);
        }
        /// <summary>
        /// GetReportList
        /// </summary>
        /// <param name="workspaceId"></param>
        /// <returns></returns>
        public async Task<List<Microsoft.PowerBI.Api.Models.Report>> GetReportList(Guid workspaceId)
        {
            // List<Report> list = new List<Report>();
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            // Get report info
            var pbiReport = await pbiClient.Reports.GetReportsInGroupAsync(workspaceId);
            var result = pbiReport.Value.ToList();
            return result;
        }
        public async Task<Pages> GetReportsPagesList(Guid workspaceId, Guid reportId)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            // Get report info
            var pbiReport = pbiClient.Reports.GetPages(workspaceId, reportId);
            // var result = pbiReport.Value.ToList();
            return pbiReport;
        }
        public async Task<bool> GetDatasetRefresh(Guid workspaceId, string datasetId)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            var res = pbiClient.Datasets.RefreshDataset(workspaceId, datasetId);
            //var result = res;
            return true;
        }
        
        public async Task<bool> GetReportSubscription(Guid reportId)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            // var result = await pbiClient.Reports.GetReportSubscriptionsAsAdminWithHttpMessagesAsync(reportId);
            var res = pbiClient.Reports.GetReportSubscriptionsAsAdmin(reportId);
            return true;
        }

        public async Task<List<Group>> GetWorkspaceInfo()
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            var pbiReport = pbiClient.Groups.GetGroups();
            var result = pbiReport.Value.Where(u => u.IsOnDedicatedCapacity == true).ToList();
            return result;

        }
        public async Task<List<Group>> GetWorkspaceInfoByOrg(int organisationId)
        {
            // 1️⃣ Get organization
            var organization = await _db.Organizations
                .FirstOrDefaultAsync(o => o.OrganizationId == organisationId);

            if (organization == null || string.IsNullOrEmpty(organization.WorkspaceId))
                return new List<Group>();

            // 2️⃣ Get Power BI client
            PowerBIClient pbiClient = await GetPowerBIClient();

            // 3️⃣ Get all workspaces
            var groupsResponse = await pbiClient.Groups.GetGroupsAsync();
            var groups = groupsResponse.Value;

            // 4️⃣ Filter only the organization workspace
            var orgWorkspaces = groups
                .Where(g =>
                    g.Id.ToString().Equals(organization.WorkspaceId, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return orgWorkspaces;
        }

        public async Task<List<string>> GetSubscriptions()
        {
            List<string> lst = new List<string>();
            var credential = new ClientSecretCredential(
                tenantId: azureAd.Value.TenantId,
                clientId: azureAd.Value.ClientId,
                clientSecret: azureAd.Value.ClientSecret
                );
            //var accessToken = await aadService.GetAccessToken();

            var client = new ArmClient(credential);

            // List subscriptions the SPN has access to
            await foreach (var sub in client.GetSubscriptions().GetAllAsync())
            {
                Console.WriteLine($"Name: {sub.Data.DisplayName}, ID: {sub.Data.SubscriptionId}");
                lst.Add($"Name: {sub.Data.DisplayName}, ID: {sub.Data.SubscriptionId}");
            }
            return lst;
        }
        public async Task<List<Refresh>> GetDatasetHistory(Guid workspaceId, string datasetId, int top = 5)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            var res = pbiClient.Datasets.GetRefreshHistory(workspaceId, datasetId, top);
            var result = res.Value.ToList();
            return result;
            //return res;
        }

        /// <summary>
        /// Get embed params for a report
        /// </summary>
        /// <returns>Wrapper object containing Embed token, Embed URL, Report Id, and Report name for single report</returns>
        public async Task<EmbedParams> GetEmbedParams(
        Guid workspaceId,
        Guid reportId,
        string userEmail,
        string type,
        [Optional] string token,
        [Optional] Guid additionalDatasetId)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            var reportData = await _db.NavigationManagements
                .Where(u => u.ReportId == reportId.ToString())
                .FirstOrDefaultAsync();

            // Get report info (sync or async both ok)
            var pbiReport = await pbiClient.Reports.GetReportInGroupAsync(workspaceId, reportId);

            EmbedToken embedToken = null;
            bool isRlsEnabled = false;                  // whether RLS is configured/expected for this report
            bool effectiveIdentityApplied = false;      // whether the embed token actually contains an effective identity

            // If paginated, use the RDL path (your helper)
            if (reportData.Type == "PaginatedReport")
            {
                embedToken = await GetEmbedTokenForRDLReportV2_OnlyReportWorkspaceAsync(pbiClient, workspaceId, reportId);
                // Paginated reports typically do not accept effective identities the same way - mark accordingly
                isRlsEnabled = false;
                effectiveIdentityApplied = false;
            }
            else if(reportData.Type == "Report")
            {
                var datasetIds = new List<string>();
                if (!string.IsNullOrEmpty(pbiReport.DatasetId))
                    datasetIds.Add(pbiReport.DatasetId);
                if (additionalDatasetId != Guid.Empty)
                    datasetIds.Add(additionalDatasetId.ToString());
                // Build GenerateTokenRequestV2 with identities
                var datasetsForV2 = datasetIds.Select(d => new GenerateTokenRequestV2Dataset(d)).ToList();
                var reportsForV2 = new List<GenerateTokenRequestV2Report> { new GenerateTokenRequestV2Report(reportId, allowEdit: false) };
                // fallback: attempt token without identities (will likely be rejected if RLS required)
                var tokenRequestFallback = new GenerateTokenRequestV2(reports: reportsForV2, datasets: datasetsForV2);
                embedToken = await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequestFallback);
                effectiveIdentityApplied = false;
            }
            else
            {
                var identity = new EffectiveIdentity(
                    username: userEmail,                     // MUST match RLS DAX
                    roles: new List<string> { "Role" },  // EXACT role name
                    datasets: new List<string> { reportData.DatasetId.ToString() }
                );

                var tokenRequest = new GenerateTokenRequestV2(
                    reports: new List<GenerateTokenRequestV2Report>
                    {
                        new GenerateTokenRequestV2Report(reportId)
                    },
                        datasets: new List<GenerateTokenRequestV2Dataset>
                    {
                        new GenerateTokenRequestV2Dataset(reportData.DatasetId.ToString())
                    },
                    targetWorkspaces: new List<GenerateTokenRequestV2TargetWorkspace>
                    {
                        new GenerateTokenRequestV2TargetWorkspace(workspaceId)
                    },
                    identities: new List<EffectiveIdentity> { identity }
                );

                embedToken =  await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequest);
                // Paginated reports typically do not accept effective identities the same way - mark accordingly
                isRlsEnabled = true;
                effectiveIdentityApplied = true;
            }

            // Build embed report list
            var embedReports = new List<EmbedReport>()
            {
                new EmbedReport
                {
                    ReportId = pbiReport.Id,
                    ReportName = pbiReport.Name,
                    EmbedUrl = pbiReport.EmbedUrl
                }
            };

            // Return EmbedParams including RLS/info flags so React can react
            var embedParams = new EmbedParams
            {
                EmbedReport = embedReports,
                Type = "Report",
                EmbedToken = embedToken,
                DatasetId = pbiReport.DatasetId,
                ReportName = reportData?.Name ?? pbiReport.Name,
                ReportDiscription = reportData?.Description ?? string.Empty,

                // NEW flags (add these properties to your EmbedParams DTO if not already present)
                IsRlsEnabled = isRlsEnabled,
                IsUserAllowed = effectiveIdentityApplied
            };

            return embedParams;
        }

        public async Task<EmbedParams> GetEmbedParamsV2(Guid workspaceId, Guid reportId, string userEmail, string type, [Optional] string token, [Optional] Guid additionalDatasetId)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Get report info
            var pbiReport = pbiClient.Reports.GetReportInGroup(workspaceId, reportId);

            //  Check if dataset is present for the corresponding report
            //  If isRDLReport is true then it is a RDL Report 
            var isRDLReport = String.IsNullOrEmpty(pbiReport.DatasetId);

            EmbedToken embedToken;

            // Generate embed token for RDL report if dataset is not present
            if (type == "PaginatedReport" )
            {
                // Get Embed token for RDL Report
                embedToken = await GetEmbedTokenForRDLReport(workspaceId, reportId);
            }
            else
            {
                // Create list of datasets
                var datasetIds = new List<Guid>();

                // Add dataset associated to the report
                datasetIds.Add(Guid.Parse(pbiReport.DatasetId));

                // Append additional dataset to the list to achieve dynamic binding later
                if (additionalDatasetId != Guid.Empty)
                {
                    datasetIds.Add(additionalDatasetId);
                }

                // Get Embed token multiple resources
                embedToken = await GetEmbedToken(reportId, datasetIds, workspaceId);
                //embedToken = await GetEmbedTokenRLS(reportId, datasetIds, token, userEmail, workspaceId);
            }

            // Add report data for embedding
            var embedReports = new List<EmbedReport>() {
                new EmbedReport
                {
                    ReportId = pbiReport.Id, ReportName = pbiReport.Name, EmbedUrl = pbiReport.EmbedUrl
                }
            };

            // Capture embed params
            var embedParams = new EmbedParams
            {
                EmbedReport = embedReports,
                Type = "Report",
                EmbedToken = embedToken,
                DatasetId = pbiReport.DatasetId,
                ReportName = pbiReport.Name,
                ReportDiscription =pbiReport.Description,

                // NEW flags (add these properties to your EmbedParams DTO if not already present)
                IsRlsEnabled = false,
                IsUserAllowed = false
            };

            return embedParams;
        }

        /// <summary>
        /// Get embed params for multiple reports for a single workspace
        /// </summary>
        /// <returns>Wrapper object containing Embed token, Embed URL, Report Id, and Report name for multiple reports</returns>
        /// <remarks>This function is not supported for RDL Report</remakrs>
        public async Task<EmbedParams> GetEmbedParams(Guid workspaceId, IList<Guid> reportIds, [Optional] IList<Guid> additionalDatasetIds)
        {

            // Note: This method is an example and is not consumed in this sample app

            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Create mapping for reports and Embed URLs
            var embedReports = new List<EmbedReport>();

            // Create list of datasets
            var datasetIds = new List<Guid>();

            // Get datasets and Embed URLs for all the reports
            foreach (var reportId in reportIds)
            {
                // Get report info
                var pbiReport = pbiClient.Reports.GetReportInGroup(workspaceId, reportId);

                datasetIds.Add(Guid.Parse(pbiReport.DatasetId));

                // Add report data for embedding
                embedReports.Add(new EmbedReport { ReportId = pbiReport.Id, ReportName = pbiReport.Name, EmbedUrl = pbiReport.EmbedUrl });
            }

            // Append to existing list of datasets to achieve dynamic binding later
            if (additionalDatasetIds != null)
            {
                datasetIds.AddRange(additionalDatasetIds);
            }

            // Get Embed token multiple resources
            var embedToken = await GetEmbedToken(reportIds, datasetIds, workspaceId);

            // Capture embed params
            var embedParams = new EmbedParams
            {
                EmbedReport = embedReports,
                Type = "Report",
                EmbedToken = embedToken
            };

            return embedParams;
        }

        public async Task<Stream> ExportReportAsync(Guid workspaceId, Guid reportId, ExportReportRequest request)
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Start export operation
            var export = await pbiClient.Reports.ExportToFileInGroupAsync(workspaceId, reportId, request);

            // Wait for export to complete (polling)
            Export exportStatus;
            do
            {
                await Task.Delay(1000); // Wait 1 second between polls
                exportStatus = await pbiClient.Reports.GetExportToFileStatusInGroupAsync(workspaceId, reportId, export.Id);
            }
            while (exportStatus.Status == ExportState.NotStarted || exportStatus.Status == ExportState.Running);

            if (exportStatus.Status == ExportState.Succeeded)
            {
                // Download exported file as stream
                var fileStream = await pbiClient.Reports.GetFileOfExportToFileInGroupAsync(workspaceId, reportId, export.Id);
                return fileStream;
            }
            else
            {
                throw new Exception($"Export failed with status: {exportStatus.Status}");
            }
        }

        public async Task<Stream> ExportReportWithGroupAsync(Guid workspaceId, Guid reportId, DownloadType? downloadType = null)
        {
            // Get PowerBIClient instance (assume you have a method for this)
            PowerBIClient pbiClient = await this.GetPowerBIClient();
            var exportConfiguration = new PowerBIReportExportConfiguration
            {
                // 1. Settings: Include hidden pages (optional, default is false)
                Settings = new ExportReportSettings
                {
                    IncludeHiddenPages = false // Only export pages visible in the report navigation
                }
                // No need to set Pages or Identities for a default full report export
            };
            // Use this configuration in your request
            var exportRequest = new ExportReportRequest
            {
                Format = FileFormat.PDF,
                PowerBIReportConfiguration = exportConfiguration
            };
            // Export the report as a stream
            Stream reportStream = await pbiClient.Reports.ExportReportAsync(workspaceId, reportId, downloadType);

            return reportStream;
        }

        /// <summary>
        /// Get Embed token for single report, multiple datasets, and an optional target workspace
        /// </summary>
        /// <returns>Embed token</returns>
        /// <remarks>This function is not supported for RDL Report</remakrs>
        public async Task<EmbedToken> GetEmbedToken(Guid reportId, IList<Guid> datasetIds, [Optional] Guid targetWorkspaceId)
        {

            //  var acces = aadService.GetEmbedTokenAsync();
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Create report object with edit enabled
            var report = new GenerateTokenRequestV2Report(reportId)
            {
                AllowEdit = true   // 👈 this replaces accessLevel: "Edit"
            };


            // Create a request for getting Embed token 
            // This method works only with new Power BI V2 workspace experience
            var tokenRequest = new GenerateTokenRequestV2(

                // reports: new List<GenerateTokenRequestV2Report>() { new GenerateTokenRequestV2Report(reportId) },
                reports: new List<GenerateTokenRequestV2Report>() { report },

                datasets: datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList(),

                targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId) } : null
                // , identities: new List<EffectiveIdentity> { rlsIdentity }
                );

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return embedToken;
        }

        public async Task<EmbedToken> GetEmbedTokenRLS(Guid reportId, IList<Guid> datasetIds, string accessToken, string userEmail, [Optional] Guid targetWorkspaceId)
        {

            //  var acces = aadService.GetEmbedTokenAsync();
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Create report object with edit enabled
            var report = new GenerateTokenRequestV2Report(reportId)
            {
                AllowEdit = true   // 👈 this replaces accessLevel: "Edit"
            };

            //// Defines the user identity and roles.
            var rlsIdentity1 = new EffectiveIdentity(
                username: userEmail,
                roles: new List<string> { "role" },
                datasets: datasetIds.Select(id => id.ToString()).ToList()
              );

            //string userGraphToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IkpZaEFjVFBNWl9MWDZEQmxPV1E3SG4wTmVYRSIsImtpZCI6IkpZaEFjVFBNWl9MWDZEQmxPV1E3SG4wTmVYRSJ9.eyJhdWQiOiJodHRwczovL2FuYWx5c2lzLndpbmRvd3MubmV0L3Bvd2VyYmkvYXBpIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvMWEyNDNjNTgtZTI2Mi00YmYzLThhOTctMDhlMmI3MzNmODgwLyIsImlhdCI6MTc1NzYwNTUzNiwibmJmIjoxNzU3NjA1NTM2LCJleHAiOjE3NTc2MDk1NDMsImFjY3QiOjAsImFjciI6IjEiLCJhaW8iOiJBWFFBaS84WkFBQUFWckcrMjl5WEEzdEVpMmNIWGVsUnhQc0MxQklDOVY3NjdJbWVSNlBQS2IxYUU3WmxZK2JxQjdYQWk5eHdWQy9nbWZnYm5BdE5yUllqbTJrZTJzYUdjYWxaNDlnS254S1c1K3hKTm1Ca01VUlpqV3BwenhyNm96UnlZWFlWODRIQmRhQUpXeFlSWWUvOHUxU2hZamMzZEE9PSIsImFtciI6WyJwd2QiLCJtZmEiXSwiYXBwaWQiOiIxOGZiY2ExNi0yMjI0LTQ1ZjYtODViMC1mN2JmMmIzOWIzZjMiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IkJhbnNhbCIsImdpdmVuX25hbWUiOiJQYW5rYWogS3VtYXIiLCJpZHR5cCI6InVzZXIiLCJpcGFkZHIiOiIyNDA1OjIwMTo0MDEzOjU4NTA6NzkwNjo2Y2RiOmI5OGY6YzE5NCIsIm5hbWUiOiJQYW5rYWogS3VtYXIgQmFuc2FsIiwib2lkIjoiNTMyOWMyMzQtNjNkMC00ZDVmLTljM2MtZDk3YTcxMGE3MWM4Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIwNTM1NzY0NTktMjE4MTkxNzU2LTEyMzE3NTQ2NjEtNDI3NTEiLCJwdWlkIjoiMTAwMzIwMDNGMzk0QzcxQSIsInJoIjoiMS5BVzRBV0R3a0dtTGk4MHVLbHdqaXR6UDRnQWtBQUFBQUFBQUF3QUFBQUFBQUFBQmVBV0Z1QUEuIiwic2NwIjoiQXBwLlJlYWQuQWxsIENhcGFjaXR5LlJlYWQuQWxsIENhcGFjaXR5LlJlYWRXcml0ZS5BbGwgQ29ubmVjdGlvbi5SZWFkLkFsbCBDb25uZWN0aW9uLlJlYWRXcml0ZS5BbGwgQ29udGVudC5DcmVhdGUgRGFzaGJvYXJkLlJlYWQuQWxsIERhc2hib2FyZC5SZWFkV3JpdGUuQWxsIERhdGFmbG93LlJlYWQuQWxsIERhdGFmbG93LlJlYWRXcml0ZS5BbGwgRGF0YXNldC5SZWFkLkFsbCBEYXRhc2V0LlJlYWRXcml0ZS5BbGwgR2F0ZXdheS5SZWFkLkFsbCBHYXRld2F5LlJlYWRXcml0ZS5BbGwgSXRlbS5FeGVjdXRlLkFsbCBJdGVtLkV4dGVybmFsRGF0YVNoYXJlLkFsbCBJdGVtLlJlYWRXcml0ZS5BbGwgSXRlbS5SZXNoYXJlLkFsbCBPbmVMYWtlLlJlYWQuQWxsIE9uZUxha2UuUmVhZFdyaXRlLkFsbCBQaXBlbGluZS5EZXBsb3kgUGlwZWxpbmUuUmVhZC5BbGwgUGlwZWxpbmUuUmVhZFdyaXRlLkFsbCBSZXBvcnQuUmVhZFdyaXRlLkFsbCBSZXBydC5SZWFkLkFsbCBTdG9yYWdlQWNjb3VudC5SZWFkLkFsbCBTdG9yYWdlQWNjb3VudC5SZWFkV3JpdGUuQWxsIiwic2lkIjoiMDA4OWFhNDktYzViZi04Zjc5LWI0NTItN2VmMTcyM2RhNDRiIiwic2lnbmluX3N0YXRlIjpbImttc2kiXSwic3ViIjoick44eDRKMURHNDVqOUhjSmdyVTZ0VUF1LUZOeFlSWWpfQnJDUFMxUy16SSIsInRpZCI6IjFhMjQzYzU4LWUyNjItNGJmMy04YTk3LTA4ZTJiNzMzZjg4MCIsInVuaXF1ZV9uYW1lIjoicGJhbnNhbEBpdGNvbnZlcmdlbmNlLmNvbSIsInVwbiI6InBiYW5zYWxAaXRjb252ZXJnZW5jZS5jb20iLCJ1dGkiOiJTQm5SWXN0YzZFQ0VOU1lqOEVsT0FBIiwidmVyIjoiMS4wIiwid2lkcyI6WyJiNzlmYmY0ZC0zZWY5LTQ2ODktODE0My03NmIxOTRlODU1MDkiXSwieG1zX2Z0ZCI6ImdFZVNxRTZIcTFid3hiV3hLUzg0dTJyUFI3dlZfcXVQUWtVZEkzdHRFc0lCZFhOemIzVjBhQzFrYzIxeiIsInhtc19pZHJlbCI6IjEgMjYifQ.lfh-BmAWxehexh9pbRiuclxyX7sSE5xmz34fbzeou9WYDFBsD6JawjfHO61ZQiZhw0trRgRkdrq3G56BHIXqgbpVepd-O7nfEDDAA054LFTRwgh4VQKdACR_m8AOg6X1RrUh3p1OiHo4xmdVgP5fR3CUBLXn2zPHmdr1iy9SsqqttGfwqyWfh9Sg-Pt60a5gvefUGYJWYNr1KeM5p7PYicDI37jDM_57hzUwwI8rtO6zAh9StDkyJMXf5eyS1yGr-Wvw-5v7Y5VORMPyiYexTKoPv8mr-F-ZIJIBq7FyEfPWtW08spxysxLzpdPKDkZioafFvsDyRs9iePo4t0kbWw";

            string userGraphToken = accessToken;

            var oboApp = ConfidentialClientApplicationBuilder.Create(azureAd.Value.ClientId)
                .WithClientSecret(azureAd.Value.ClientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{azureAd.Value.TenantId}")
                .Build();

            string[] userScopes = new[] { "https://analysis.windows.net/powerbi/api/.default" };

            var oboResult = await oboApp.AcquireTokenOnBehalfOf(userScopes,
                new Microsoft.Identity.Client.UserAssertion(userGraphToken)).ExecuteAsync();

            string userPbiToken = oboResult.AccessToken;
            IdentityBlob identityBlob = new IdentityBlob(
                                        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(userPbiToken))
                                    );

            var rlsIdentity = new EffectiveIdentity(  // If dynamic RLS
                username: userEmail,
                roles: new List<string> { "role" },
                //customData: "Email",
                datasets: datasetIds.Select(id => id.ToString()).ToList(),
                identityBlob: identityBlob
             );


            // This method works only with new Power BI V2 workspace experience
            var tokenRequest = new GenerateTokenRequestV2(

                reports: new List<GenerateTokenRequestV2Report>() { new GenerateTokenRequestV2Report(reportId) },
                // reports: new List<GenerateTokenRequestV2Report>() { report },

                datasets: datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList(),

                targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId) } : null
                , identities: new List<EffectiveIdentity> { rlsIdentity }

                );

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return embedToken;
        }

        public async Task<EmbedToken> GetEmbedTokenRLSV2(
        Guid reportId,
        IList<Guid> datasetIds,
        string userEmail,
        Guid workspaceId)
        {
            var pbiClient = await GetPowerBIClient();

            var identity = new EffectiveIdentity(
                username: userEmail,
                datasets: datasetIds.Select(id => id.ToString()).ToList()
            );

            var tokenRequest = new GenerateTokenRequestV2(
                reports: new List<GenerateTokenRequestV2Report>
                {
            new GenerateTokenRequestV2Report(reportId)
                },
                datasets: datasetIds.Select(d => new GenerateTokenRequestV2Dataset(d.ToString())).ToList(),
                targetWorkspaces: new List<GenerateTokenRequestV2TargetWorkspace>
                {
            new GenerateTokenRequestV2TargetWorkspace(workspaceId)
                },
                identities: new List<EffectiveIdentity> { identity }
            );

            return pbiClient.EmbedToken.GenerateToken(tokenRequest);
        }

        public async Task<EmbedToken> GenerateV2EmbedTokenAsync(
        Guid workspaceId,
        Guid reportId,
        string userEmail,
        IList<Guid> datasetIds,
        Guid? targetWorkspaceId = null)
        {
            var pbiClient = await GetPowerBIClient();

            // -----------------------------------------------------
            // 1. Define the reports section
            // -----------------------------------------------------
            var reports = new List<GenerateTokenRequestV2Report>
            {
                new GenerateTokenRequestV2Report(reportId)
                {
                    AllowEdit = false  // true if you want edit mode
                }
            };

            // -----------------------------------------------------
            // 2. Define the datasets section
            // -----------------------------------------------------
            var datasets = datasetIds
                .Select(id => new GenerateTokenRequestV2Dataset(id.ToString()))
                .ToList();

            // -----------------------------------------------------
            // 3. Define target workspace (optional)
            // -----------------------------------------------------
            List<GenerateTokenRequestV2TargetWorkspace>? targetWorkspaces = null;

            if (targetWorkspaceId.HasValue)
            {
                targetWorkspaces = new List<GenerateTokenRequestV2TargetWorkspace>
                {
                    new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId.Value)
                };
            }

            // -----------------------------------------------------
            // 4. Define EffectiveIdentity for RLS
            // -----------------------------------------------------
            List<EffectiveIdentity>? identities = null;

            if (!string.IsNullOrWhiteSpace(userEmail))
            {
                identities = new List<EffectiveIdentity>
                {
                    new EffectiveIdentity(
                        username: userEmail,
                        datasets: datasetIds.Select(id => id.ToString()).ToList()
                    )
                };
            }

            // -----------------------------------------------------
            // 5. Build V2 token request
            // -----------------------------------------------------
            var tokenRequest = new GenerateTokenRequestV2(
                reports: reports,
                datasets: datasets,
                targetWorkspaces: targetWorkspaces,
                identities: identities
            );

            // -----------------------------------------------------
            // 6. Call Power BI REST API
            // -----------------------------------------------------
            var embedToken = await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequest);

            return embedToken;
        }


        public async Task<EmbedToken> GenerateEmbedTokenWithRlsAsync(
        Guid workspaceId,
        Guid reportId,
        Guid datasetId,
        string userEmail
)
        {
            var pbiClient = await GetPowerBIClient();

            var identity = new EffectiveIdentity(
                username: userEmail,                     // MUST match RLS DAX
                roles: new List<string> { "Role" },  // EXACT role name
                datasets: new List<string> { datasetId.ToString() }
            );

            var tokenRequest = new GenerateTokenRequestV2(
                reports: new List<GenerateTokenRequestV2Report>
                {
            new GenerateTokenRequestV2Report(reportId)
                },
                datasets: new List<GenerateTokenRequestV2Dataset>
                {
            new GenerateTokenRequestV2Dataset(datasetId.ToString())
                },
                targetWorkspaces: new List<GenerateTokenRequestV2TargetWorkspace>
                {
            new GenerateTokenRequestV2TargetWorkspace(workspaceId)
                },
                identities: new List<EffectiveIdentity> { identity }
            );

            return await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequest);
        }

        public async Task<bool> DatasetHasRLSAsync(Guid workspaceId, Guid datasetId, string accessToken)
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                // -----------------------------
                // TRY FABRIC SEMANTIC MODEL API
                // -----------------------------
                var fabricUrl =
                    $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/semanticmodels/{datasetId}/roles";

                var fabricResp = await http.GetAsync(fabricUrl);
                string fabricBody = await fabricResp.Content.ReadAsStringAsync();

                if (fabricResp.IsSuccessStatusCode)
                {
                    var fabricResult = JsonConvert.DeserializeObject<DatasetRolesResponse>(fabricBody);
                    bool hasRoles = fabricResult?.Value != null && fabricResult.Value.Any();

                    Console.WriteLine($"FABRIC RLS DETECTED = {hasRoles}");
                    return hasRoles;
                }

                // If Fabric returns 404 or 400, try PBIX endpoint
                Console.WriteLine($"Fabric roles endpoint failed: {fabricResp.StatusCode} | {fabricBody}");

                // -----------------------------
                // TRY CLASSIC DATASET API
                // -----------------------------
                var pbixUrl =
                    $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/roles";

                var pbixResp = await http.GetAsync(pbixUrl);
                string pbixBody = await pbixResp.Content.ReadAsStringAsync();

                if (pbixResp.IsSuccessStatusCode)
                {
                    var pbixResult = JsonConvert.DeserializeObject<DatasetRolesResponse>(pbixBody);
                    bool hasRoles = pbixResult?.Value != null && pbixResult.Value.Any();

                    Console.WriteLine($"PBIX RLS DETECTED = {hasRoles}");
                    return hasRoles;
                }

                // -----------------------------
                // If both fail — assume FABRIC has RLS but REST does not expose it
                // -----------------------------
                Console.WriteLine("⚠ WARNING: Both Fabric and PBIX role endpoints failed. " +
                                  "Assuming FABRIC semantic model with invisible RLS.");

                return true; // Fabric models often hide RLS in REST API.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 ERROR in RLS detection: {ex.Message}");
                return true; // Fallback to true so EffectiveIdentity applies.
            }
        }

        public class DatasetRolesResponse
        {
            public List<DatasetRole> Value { get; set; }
        }

        public class DatasetRole
        {
            public string Name { get; set; }
        }

        /// <summary>
        /// Get Embed token for multiple reports, datasets, and an optional target workspace
        /// </summary>
        /// <returns>Embed token</returns>
        /// <remarks>This function is not supported for RDL Report</remakrs>
        public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] Guid targetWorkspaceId)
        {
            // Note: This method is an example and is not consumed in this sample app

            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Convert report Ids to required types
            // var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();
            var reports = reportIds
               .Select(reportId => new GenerateTokenRequestV2Report(reportId) { AllowEdit = true })
               .ToList();
            // Convert dataset Ids to required types
            var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

            // Create a request for getting Embed token 
            // This method works only with new Power BI V2 workspace experience
            var tokenRequest = new GenerateTokenRequestV2(

                datasets: datasets,

                reports: reports,

                targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId) } : null

                );

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return embedToken;
        }

        /// <summary>
        /// Get Embed token for multiple reports, datasets, and optional target workspaces
        /// </summary>
        /// <returns>Embed token</returns>
        /// <remarks>This function is not supported for RDL Report</remakrs>
        public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] IList<Guid> targetWorkspaceIds)
        {
            // Note: This method is an example and is not consumed in this sample app

            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Convert report Ids to required types
            var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();

            // Convert dataset Ids to required types
            var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

            // Convert target workspace Ids to required types
            IList<GenerateTokenRequestV2TargetWorkspace> targetWorkspaces = null;
            if (targetWorkspaceIds != null)
            {
                targetWorkspaces = targetWorkspaceIds.Select(targetWorkspaceId => new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId)).ToList();
            }

            // Create a request for getting Embed token 
            // This method works only with new Power BI V2 workspace experience
            var tokenRequest = new GenerateTokenRequestV2(

                datasets: datasets,

                reports: reports,

                targetWorkspaces: targetWorkspaceIds != null ? targetWorkspaces : null
            );

            // Generate Embed token
            var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

            return embedToken;
        }

        /// <summary>
        /// Get Embed token for RDL Report
        /// </summary>
        /// <returns>Embed token</returns>
        public async Task<EmbedToken> GetEmbedTokenForRDLReport(Guid targetWorkspaceId, Guid reportId, string accessLevel = "view")
        {
            PowerBIClient pbiClient = await this.GetPowerBIClient();

            // Generate token request for RDL Report
            var generateTokenRequestParameters = new GenerateTokenRequest(
                accessLevel: accessLevel
            );

            // Generate Embed token
            var embedToken = pbiClient.Reports.GenerateTokenInGroup(targetWorkspaceId, reportId, generateTokenRequestParameters);

            return embedToken;
        }

        
        public async Task<EmbedToken> GetEmbedTokenForRDLReportV2_OnlyReportWorkspaceAsync(
        PowerBIClient pbiClient,
        Guid reportWorkspaceId, // e.g. c0cb7599-ec65-415f-a845-cc8fc3062be6
        Guid reportId,
        string accessLevel = "View")
        {
            var report = await pbiClient.Reports.GetReportInGroupAsync(reportWorkspaceId, reportId);
            Console.WriteLine($"ReportType: {report.ReportType}");
            Console.WriteLine($"Report.DatasetId (may be empty for RDL): {report.DatasetId ?? "<null>"}");

            // get datasources from paginated report
            var dsResp = await pbiClient.Reports.GetDatasourcesInGroupAsync(reportWorkspaceId, reportId);
            var datasourceList = dsResp.Value ?? new List<Datasource>();

            // extract GUIDs (from connectionDetails.database and datasourceId)
            var guidRegex = new Regex(@"[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}", RegexOptions.Compiled);
            var datasetGuids = new HashSet<Guid>();

            foreach (var ds in datasourceList)
            {
                try
                {
                    // connectionDetails -> database string may contain GUID
                    var connProp = ds.GetType().GetProperty("ConnectionDetails");
                    if (connProp != null)
                    {
                        var connVal = connProp.GetValue(ds);
                        if (connVal != null)
                        {
                            var cs = JsonConvert.SerializeObject(connVal);
                            foreach (Match m in guidRegex.Matches(cs))
                                if (Guid.TryParse(m.Value, out var g) && g != Guid.Empty) datasetGuids.Add(g);
                        }
                    }

                    // datasourceId property may be present
                    var dsIdProp = ds.GetType().GetProperty("DatasourceId") ?? ds.GetType().GetProperty("DataSourceId");
                    if (dsIdProp != null)
                    {
                        var dsIdVal = dsIdProp.GetValue(ds)?.ToString();
                        if (!string.IsNullOrEmpty(dsIdVal) && Guid.TryParse(dsIdVal, out var g) && g != Guid.Empty) datasetGuids.Add(g);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning parsing datasource: {ex.Message}");
                }
            }

            // fallback to report.DatasetId if present
            if (!string.IsNullOrEmpty(report.DatasetId) && Guid.TryParse(report.DatasetId, out var repDs) && repDs != Guid.Empty)
                datasetGuids.Add(repDs);

            // If there are no Power BI dataset GUIDs, use V1 token (no datasets)
            if (datasetGuids.Count == 0)
            {
                var genParams = new GenerateTokenRequest(accessLevel: accessLevel);
                return await pbiClient.Reports.GenerateTokenInGroupAsync(reportWorkspaceId, reportId, genParams);
            }

            Console.WriteLine("Found dataset GUIDs: " + string.Join(", ", datasetGuids));

            // NOW: check ONLY the report workspace for these datasets
            var missingInReportWorkspace = new List<Guid>();
            foreach (var dsId in datasetGuids)
            {
                try
                {
                    var dsCheck = await pbiClient.Datasets.GetDatasetInGroupAsync(reportWorkspaceId, dsId.ToString());
                    // found in report workspace - good
                    Console.WriteLine($"Dataset {dsId} exists in report workspace {reportWorkspaceId}");
                }
                catch (Microsoft.Rest.HttpOperationException ex)
                {
                    // 404 or other -> treat as missing
                    Console.WriteLine($"Dataset {dsId} not found in report workspace {reportWorkspaceId}: {ex.Response?.StatusCode}");
                    missingInReportWorkspace.Add(dsId);
                }
            }

            if (missingInReportWorkspace.Count > 0)
            {
                // Build clear error message and throw (or return a structured error result as you prefer)
                var missingList = string.Join(", ", missingInReportWorkspace);
                var msg = $@"One or more Power BI datasets referenced by the paginated report are NOT present in the report workspace ({reportWorkspaceId}).
Missing dataset ids: {missingList}

To fix this (choose one):
1) Move or repoint the referenced datasets into the report workspace ({reportWorkspaceId}).
   - In Power BI service: open the dataset, use 'Save a copy' into the target workspace OR republish the dataset to the report workspace.
2) Grant your embedding identity (service principal or user) Viewer access on the workspace(s) that host the datasets, and include those workspace ids in targetWorkspaces when creating the V2 embed token.
   - If you cannot change the report to reference datasets in the same workspace, you MUST add the workspace(s) that contain these datasets to targetWorkspaces.

Example admin action (Power BI portal):
 - Go to the workspace that contains dataset X
 - Workspace -> Access -> Add -> enter the service principal/app or user -> assign 'Viewer' or above

After doing one of the above, re-run token generation.";

                // Log for admins
                Console.WriteLine(msg);
                throw new InvalidOperationException(msg);
            }

            // All datasets are present in the single report workspace -> build V2 token with that workspace only
            var v2Datasets = new List<GenerateTokenRequestV2Dataset>();
            foreach (var d in datasetGuids)
            {
                var dsEntry = new GenerateTokenRequestV2Dataset();
                // set Id (SDK may expect Guid or string)
                var idProp = dsEntry.GetType().GetProperty("Id");
                if (idProp.PropertyType == typeof(Guid) || idProp.PropertyType == typeof(Guid?))
                    idProp.SetValue(dsEntry, d);
                else
                    idProp.SetValue(dsEntry, d.ToString());

                // set XmlaPermissions (enum or string)
                var xmlaProp = dsEntry.GetType().GetProperty("XmlaPermissions");
                if (xmlaProp != null)
                {
                    var propType = Nullable.GetUnderlyingType(xmlaProp.PropertyType) ?? xmlaProp.PropertyType;
                    if (propType.IsEnum)
                    {
                        var enumValue = Enum.Parse(propType, "ReadOnly", ignoreCase: true);
                        xmlaProp.SetValue(dsEntry, enumValue);
                    }
                    else if (xmlaProp.PropertyType == typeof(string))
                    {
                        xmlaProp.SetValue(dsEntry, "ReadOnly");
                    }
                }

                // set allowEdit if present
                var allowEditProp = dsEntry.GetType().GetProperty("AllowEdit");
                if (allowEditProp != null && (allowEditProp.PropertyType == typeof(bool) || allowEditProp.PropertyType == typeof(bool?)))
                    allowEditProp.SetValue(dsEntry, false);

                v2Datasets.Add(dsEntry);
            }

            // targetWorkspaces: only the report workspace
            var v2TargetWorkspaces = new List<GenerateTokenRequestV2TargetWorkspace> { new GenerateTokenRequestV2TargetWorkspace(reportWorkspaceId) };

            var v2Reports = new List<GenerateTokenRequestV2Report> { new GenerateTokenRequestV2Report(reportId, allowEdit: false) };
            var tokenRequestV2 = new GenerateTokenRequestV2(datasets: v2Datasets, reports: v2Reports, targetWorkspaces: v2TargetWorkspaces);

            // debug JSON
            Console.WriteLine("Final tokenRequestV2 JSON:\n" + JsonConvert.SerializeObject(tokenRequestV2, Formatting.Indented,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Include }));

            // generate token
            var embedToken = await pbiClient.EmbedToken.GenerateTokenAsync(tokenRequestV2);
            return embedToken;
        }

        public async Task<EmbedToken> GenerateRdlTokenManual(Guid workspaceId, Guid reportId)
        {
            var client = await GetPowerBIClient(); // SPN token provider

            string url = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/rdlreports/{reportId}/GenerateToken";

            var request = new GenerateTokenRequest("view");

            var response = await client.HttpClient.PostAsJsonAsync(url, request);

            var result = await response.Content.ReadFromJsonAsync<EmbedToken>();

            return result;
        }

        public async Task<IEnumerable<RefreshableInfo>> ListAllRefreshablesAsync(int top, string expand, string filter, int skip)
        {
            using (var client = await this.GetPowerBIClient())
            {
                try
                {
                    var refreshableCollection = await client.Admin.GetRefreshablesAsync(top, expand, filter, skip);

                    if (refreshableCollection?.Value == null)
                    {
                        return Enumerable.Empty<RefreshableInfo>();
                    }

                    return refreshableCollection.Value
                        .Select(r => new RefreshableInfo
                        {
                            Id = r.Id,
                            Name = r.Name,
                            Kind = r.Kind,
                            StartTime = r.StartTime,
                            EndTime = r.EndTime,
                            RefreshCount = r.RefreshCount,
                            RefreshFailures = r.RefreshFailures,
                            AverageDuration = r.AverageDuration,
                            MedianDuration = r.MedianDuration,
                            RefreshesPerDay = r.RefreshesPerDay,
                            ConfiguredBy = r.ConfiguredBy?.ToList(),

                            LastRefresh = r.LastRefresh == null ? null : new LastRefreshInfo
                            {
                                RefreshType = r.LastRefresh.RefreshType,
                                StartTime = r.LastRefresh.StartTime,
                                EndTime = r.LastRefresh.EndTime,
                                Status = r.LastRefresh.Status,
                                RequestId = r.LastRefresh.RequestId
                            },

                            RefreshSchedule = r.RefreshSchedule == null ? null : new RefreshScheduleInfo
                            {
                                Days = r.RefreshSchedule.Days?.Select(d => d.ToString()).ToList(),
                                Times = r.RefreshSchedule.Times?.ToList(),
                                Enabled = r.RefreshSchedule.Enabled,
                                LocalTimeZoneId = r.RefreshSchedule.LocalTimeZoneId,
                                NotifyOption = r.RefreshSchedule.NotifyOption
                            }
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public async Task<Refreshable?> GetSingleRefreshableRawAsync(Guid capacityId, string refreshableId)
        {
            using (var client = await CreatePowerBIClientAsync())
            {
                try
                {
                    var refreshableResponse = await client.Admin.GetRefreshableForCapacityWithHttpMessagesAsync(
                        capacityId,
                        refreshableId);

                    return refreshableResponse.Body?.Value?.FirstOrDefault();
                }
                catch (HttpOperationException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }

        public async Task<bool> SharePowerBiReportAsync(
            string workspaceId,
            string reportId,
            IEnumerable<string> emails,
            IEnumerable<string>? groupIds = null,
            bool shareToAll = false,
            bool reshare = false)   
            {
                try
                {
                    // ----------------------------
                    // 1️⃣ VALIDATION
                    // ----------------------------
                    if (string.IsNullOrWhiteSpace(workspaceId))
                        throw new ArgumentException("Workspace ID cannot be empty.");

                    if (string.IsNullOrWhiteSpace(reportId))
                        throw new ArgumentException("Report ID cannot be empty.");

                    if (emails == null || !emails.Any())
                        throw new ArgumentException("At least one email is required.");

                    // Ensure “groups/me” is NOT used (My Workspace not allowed)
                    if (workspaceId.Equals("me", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Sharing reports from 'My Workspace' is not allowed by Power BI API.");

                    // ----------------------------
                    // 2️⃣ Build the request body
                    // ----------------------------
                    var payload = new
                    {
                        emails = emails.ToArray(),
                        groupIds = groupIds?.ToArray() ?? Array.Empty<string>(),
                        shareToAll,
                        reshare
                    };

                    var content = new StringContent(
                        System.Text.Json.JsonSerializer.Serialize(payload),
                        Encoding.UTF8,
                        "application/json"
                    );

                    // ----------------------------
                    // 3️⃣ Acquire token
                    // ----------------------------
                    var credential = new ClientSecretCredential(
                        azureAd.Value.TenantId,
                        azureAd.Value.ClientId,
                        azureAd.Value.ClientSecret
                    );

                    var token = await credential.GetTokenAsync(
                        new TokenRequestContext(new[] { "https://analysis.windows.net/powerbi/api/.default" })
                    );

                    using var httpClient = new HttpClient();
                    httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token.Token);

                    // ----------------------------
                    // 4️⃣ Try main cluster first
                    // ----------------------------
                    string baseUrl = "https://api.powerbi.com/";
                    string apiPath = $"v1.0/myorg/groups/{workspaceId}/reports/{reportId}/Share";
                    string initialUrl = $"{baseUrl}{apiPath}";

                    var response = await httpClient.PostAsync(initialUrl, content);

                    // ----------------------------
                    // 5️⃣ Handle Power BI cluster reroute (403 + home-cluster-uri)
                    // ----------------------------
                    if (response.StatusCode == HttpStatusCode.Forbidden &&
                        response.Headers.TryGetValues("home-cluster-uri", out var clusterUris))
                    {
                        var clusterUri = clusterUris.FirstOrDefault();

                        if (!string.IsNullOrWhiteSpace(clusterUri))
                        {
                            // Ensure trailing slash
                            string fixedCluster =
                                clusterUri.EndsWith("/") ? clusterUri : clusterUri + "/";

                            string redirectUrl = $"{fixedCluster}{apiPath}";

                            Console.WriteLine($"🔁 Retrying on correct cluster: {fixedCluster}");

                            response = await httpClient.PostAsync(redirectUrl, content);
                        }
                    }

                    // ----------------------------
                    // 6️⃣ Return final result
                    // ----------------------------
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Power BI report shared successfully!");
                        return true;
                    }

                    string apiError = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Power BI Share failed ({response.StatusCode}): {apiError}");
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ SharePowerBiReportAsync Exception: {ex.Message}");
                    throw;
                }
            }

        public async Task SendReportShareEmail(ShareReportRequest req)
        {
            string subject = $"{req.SharedBy} shared a Power BI Report with you";

            string body = $@"
                <div style='font-family:Segoe UI, Arial;'>
                    <h2>{req.SharedBy} shared this Power BI Report with you</h2>
                    <p><b>Report Id:</b> {req.ReportId}</p>
                    <p>{req.Message}</p>

                    <a href='{req.ShareUrl}' 
                       style='padding:10px 18px; background-color:#107C41; color:white; 
                              text-decoration:none; border-radius:4px;'>
                       Open this report >
                    </a>

                    <br/><br/>

                    <p style='color:#555'>Microsoft Power BI</p>
                </div>
                ";

            await _emailService.SendEmailAsync(req.ToEmail, subject, body);
        }

        public async Task<List<BookmarkResponseDto>> GetUserBookmarksAsync(int userId, string reportId)
        {
            return await _db.PowerBI_Bookmarks
                .Where(b => b.UserId == userId && b.ReportId == reportId)
                .Select(b => new BookmarkResponseDto
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    WorkspaceId = b.WorkspaceId,
                    ReportId = b.ReportId,
                    PageName = b.PageName,
                    PageNumber = b.PageNumber,
                    BookmarkState = b.BookmarkState,
                    BookmarkName = b.BookmarkName,
                    CreatedOn = b.CreatedOn
                })
                .ToListAsync();
        }
        public async Task<bool> DeleteBookmarkAsync(long id, int userId)
        {
            var bookmark = await _db.PowerBI_Bookmarks
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

            if (bookmark == null)
                return false;

            _db.PowerBI_Bookmarks.Remove(bookmark);
            await _db.SaveChangesAsync();

            return true;
        }

        public async Task<BookmarkResponseDto> CreateBookmarkAsync(BookmarkRequestDto dto)
        {
            var entity = new PowerBiBookmark
            {
                UserId = dto.UserId,
                WorkspaceId = dto.WorkspaceId,
                ReportId = dto.ReportId,
                PageName = dto.PageName,
                PageNumber = dto.PageNumber,
                BookmarkState = dto.BookmarkState,
                BookmarkName = dto.BookmarkName
            };

            _db.PowerBI_Bookmarks.Add(entity);
            await _db.SaveChangesAsync();

            return new BookmarkResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                WorkspaceId = entity.WorkspaceId,
                ReportId = entity.ReportId,
                PageName = entity.PageName,
                PageNumber = entity.PageNumber,
                BookmarkState = entity.BookmarkState,
                BookmarkName = entity.BookmarkName,
                CreatedOn = entity.CreatedOn
            };
        }

        public async Task<BookmarkResponseDto> UpdateBookmarkAsync(UpdateBookmarkDto dto)
        {
            var bookmark = await _db.PowerBI_Bookmarks
                .FirstOrDefaultAsync(x => x.Id == dto.Id && x.UserId == dto.UserId);

            if (bookmark == null)
                return null;

            bookmark.BookmarkState = dto.BookmarkState;
            bookmark.BookmarkName = dto.BookmarkName;
            bookmark.PageName = dto.PageName;
            bookmark.PageNumber = dto.PageNumber;
            bookmark.UpdatedOn = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new BookmarkResponseDto
            {
                Id = bookmark.Id,
                UserId = bookmark.UserId,
                WorkspaceId = bookmark.WorkspaceId,
                ReportId = bookmark.ReportId,
                PageName = bookmark.PageName,
                PageNumber = bookmark.PageNumber,
                BookmarkState = bookmark.BookmarkState,
                BookmarkName = bookmark.BookmarkName,
                CreatedOn = bookmark.CreatedOn
            };
        }

    }
}
