using Azure.Core;
using Azure.Identity;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace BIEmbedSystem.Services
{
    public class DatasetTableService
    {
        private readonly AadService _aadService;
        private readonly IOptions<AzureAdSettings> _azureAd;
        private readonly ILogger<DatasetTableService> _logger;
        private readonly HttpClient _httpClient;

        private readonly string _powerBIResourceUrl = "https://analysis.windows.net/powerbi/api/.default";
        private readonly string _powerBiApiUrl = "https://api.powerbi.com";

        public DatasetTableService(
            AadService aadService,
            IOptions<AzureAdSettings> azureAd,
            ILogger<DatasetTableService> logger,
            HttpClient httpClient)
        {
            _aadService = aadService;
            _azureAd = azureAd;
            _logger = logger;
            _httpClient = httpClient;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Creates a Power BI SDK client using the service principal.</summary>
        private async Task<PowerBIClient> GetPowerBIClientAsync()
        {
            var accessToken = await _aadService.GetAccessToken();
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
            return new PowerBIClient(new Uri(_powerBiApiUrl), tokenCredentials);
        }

        /// <summary>Returns a raw Bearer token for the service principal.</summary>
        private async Task<string> GetBearerTokenAsync()
        {
            var credential = new ClientSecretCredential(
                _azureAd.Value.TenantId,
                _azureAd.Value.ClientId,
                _azureAd.Value.ClientSecret);

            var token = await credential.GetTokenAsync(
                new TokenRequestContext(new[] { _powerBIResourceUrl }));

            return token.Token;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1.  GET TABLES FOR A SPECIFIC DATASET
        //     Strategy A → executeQueries with INFO.TABLES() / INFO.COLUMNS() /
        //                   INFO.MEASURES()  DAX functions  (needs Dataset.Read.All
        //                   and workspace membership – NO admin required)
        //     Strategy B → REST datasources fallback (always available)
        // ─────────────────────────────────────────────────────────────────────

        public async Task<DatasetTablesResponseDto> GetTablesForDatasetAsync(
            Guid workspaceId,
            Guid datasetId)
        {
            _logger.LogInformation(
                "GetTablesForDataset → workspaceId={WorkspaceId}, datasetId={DatasetId}",
                workspaceId, datasetId);

            // ── Strategy A: executeQueries INFO functions ─────────────────────
            try
            {
                var xmlaResult = await GetTablesViaExecuteQueriesAsync(workspaceId, datasetId);
                if (xmlaResult != null && xmlaResult.Tables.Any())
                {
                    _logger.LogInformation(
                        "executeQueries INFO strategy succeeded for dataset {DatasetId}", datasetId);
                    return xmlaResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    "executeQueries INFO strategy failed (falling back to REST). Reason: {Reason}",
                    ex.Message);
            }

            // ── Strategy B: REST datasources fallback ─────────────────────────
            _logger.LogInformation(
                "Using REST datasource fallback for dataset {DatasetId}", datasetId);

            return await GetTablesViaRestFallbackAsync(workspaceId, datasetId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2.  GET TABLES FOR A REPORT
        // ─────────────────────────────────────────────────────────────────────

        public async Task<DatasetTablesResponseDto> GetTablesForReportAsync(
            Guid workspaceId,
            Guid reportId)
        {
            _logger.LogInformation(
                "GetTablesForReport → workspaceId={WorkspaceId}, reportId={ReportId}",
                workspaceId, reportId);

            var pbiClient = await GetPowerBIClientAsync();
            var report = await pbiClient.Reports.GetReportInGroupAsync(workspaceId, reportId);

            if (string.IsNullOrWhiteSpace(report.DatasetId))
                throw new InvalidOperationException(
                    $"Report {reportId} has no associated dataset " +
                    "(it may be a paginated RDL report with no linked Power BI dataset).");

            var datasetId = Guid.Parse(report.DatasetId);

            _logger.LogInformation(
                "Resolved datasetId={DatasetId} for reportId={ReportId}", datasetId, reportId);

            var result = await GetTablesForDatasetAsync(workspaceId, datasetId);
            result.ReportId = reportId.ToString();
            result.ReportName = report.Name;

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3.  GET TABLE SUMMARY FOR ALL REPORTS IN A WORKSPACE
        // ─────────────────────────────────────────────────────────────────────

        public async Task<List<DatasetTableSummaryDto>> GetTableSummaryForWorkspaceAsync(
            Guid workspaceId)
        {
            _logger.LogInformation(
                "GetTableSummaryForWorkspace → workspaceId={WorkspaceId}", workspaceId);

            var pbiClient = await GetPowerBIClientAsync();
            var reportsResponse = await pbiClient.Reports.GetReportsInGroupAsync(workspaceId);
            var reports = reportsResponse.Value.ToList();

            if (!reports.Any())
                return new List<DatasetTableSummaryDto>();

            var result = new List<DatasetTableSummaryDto>();

            // Cache datasets already processed so we don't hit the API twice
            // for reports that share the same dataset
            var processedDatasets = new Dictionary<string, DatasetTablesResponseDto>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var report in reports)
            {
                if (string.IsNullOrWhiteSpace(report.DatasetId))
                {
                    result.Add(new DatasetTableSummaryDto
                    {
                        ReportId = report.Id.ToString(),
                        ReportName = report.Name,
                        DatasetId = string.Empty,
                        DatasetName = "N/A (Paginated / No Dataset)",
                        TableNames = new List<string>()
                    });
                    continue;
                }

                try
                {
                    if (!processedDatasets.TryGetValue(report.DatasetId, out var datasetDto))
                    {
                        datasetDto = await GetTablesForDatasetAsync(
                            workspaceId, Guid.Parse(report.DatasetId));
                        processedDatasets[report.DatasetId] = datasetDto;
                    }

                    result.Add(new DatasetTableSummaryDto
                    {
                        ReportId = report.Id.ToString(),
                        ReportName = report.Name,
                        DatasetId = report.DatasetId,
                        DatasetName = datasetDto.DatasetName,
                        TableNames = datasetDto.Tables.Select(t => t.TableName).ToList()
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Could not fetch tables for report {ReportName} (dataset {DatasetId}): {Msg}",
                        report.Name, report.DatasetId, ex.Message);

                    result.Add(new DatasetTableSummaryDto
                    {
                        ReportId = report.Id.ToString(),
                        ReportName = report.Name,
                        DatasetId = report.DatasetId,
                        DatasetName = "Unknown",
                        TableNames = new List<string>(),
                        Error = ex.Message
                    });
                }
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // STRATEGY A – executeQueries + INFO DAX functions
        // Permissions needed: Dataset.Read.All + workspace member
        // NO admin / Tenant.Read.All required
        // ─────────────────────────────────────────────────────────────────────

        private async Task<DatasetTablesResponseDto> GetTablesViaExecuteQueriesAsync(
            Guid workspaceId,
            Guid datasetId)
        {
            var pbiClient = await GetPowerBIClientAsync();
            var dataset = await pbiClient.Datasets.GetDatasetInGroupAsync(
                workspaceId, datasetId.ToString());

            string workspaceName = await GetWorkspaceNameAsync(pbiClient, workspaceId);
            string bearerToken = await GetBearerTokenAsync();

            string baseUrl =
                $"{_powerBiApiUrl}/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/executeQueries";

            // ── Tables ────────────────────────────────────────────────────────
            var tableRows = await RunInfoQueryAsync(bearerToken, baseUrl,
                "EVALUATE SELECTCOLUMNS(" +
                "INFO.TABLES()," +
                "\"TableName\", [Name]," +
                "\"TableDescription\", [Description]," +
                "\"IsHidden\", [IsHidden])");

            if (!tableRows.Any())
                throw new InvalidOperationException(
                    "INFO.TABLES() returned no rows – dataset may not support executeQueries.");

            // ── Columns ───────────────────────────────────────────────────────
            var columnRows = await RunInfoQueryAsync(bearerToken, baseUrl,
                "EVALUATE SELECTCOLUMNS(" +
                "INFO.COLUMNS()," +
                "\"TableName\", [TableName]," +
                "\"ColumnName\", [ExplicitName]," +
                "\"DataType\", [ExplicitDataType]," +
                "\"IsHidden\", [IsHidden])");

            // ── Measures ──────────────────────────────────────────────────────
            var measureRows = await RunInfoQueryAsync(bearerToken, baseUrl,
                "EVALUATE SELECTCOLUMNS(" +
                "INFO.MEASURES()," +
                "\"TableName\", [TableName]," +
                "\"MeasureName\", [Name]," +
                "\"Expression\", [Expression]," +
                "\"IsHidden\", [IsHidden])");

            // ── Assemble ──────────────────────────────────────────────────────
            var tables = new List<DatasetTableDto>();

            foreach (var tableRow in tableRows)
            {
                string tableName = tableRow.GetValueOrDefault("TableName", string.Empty);
                if (string.IsNullOrWhiteSpace(tableName))
                    continue;

                // Skip Power BI auto-generated hidden date tables
                if (tableName.StartsWith("DateTableTemplate_") ||
                    tableName.StartsWith("LocalDateTable_"))
                    continue;

                var columns = columnRows
                    .Where(c => c.GetValueOrDefault("TableName") == tableName)
                    .Select(c => new DatasetColumnDto
                    {
                        ColumnName = c.GetValueOrDefault("ColumnName", string.Empty),
                        DataType = MapDataType(c.GetValueOrDefault("DataType", string.Empty)),
                        IsHidden = "True".Equals(
                            c.GetValueOrDefault("IsHidden", "False"),
                            StringComparison.OrdinalIgnoreCase)
                    })
                    .Where(c => !string.IsNullOrWhiteSpace(c.ColumnName))
                    .ToList();

                var measures = measureRows
                    .Where(m => m.GetValueOrDefault("TableName") == tableName)
                    .Select(m => new DatasetMeasureDto
                    {
                        MeasureName = m.GetValueOrDefault("MeasureName", string.Empty),
                        Expression = m.GetValueOrDefault("Expression", string.Empty),
                        IsHidden = "True".Equals(
                            m.GetValueOrDefault("IsHidden", "False"),
                            StringComparison.OrdinalIgnoreCase)
                    })
                    .Where(m => !string.IsNullOrWhiteSpace(m.MeasureName))
                    .ToList();

                tables.Add(new DatasetTableDto
                {
                    TableName = tableName,
                    Description = tableRow.GetValueOrDefault("TableDescription", string.Empty),
                    IsHidden = "True".Equals(
                        tableRow.GetValueOrDefault("IsHidden", "False"),
                        StringComparison.OrdinalIgnoreCase),
                    Columns = columns,
                    Measures = measures
                });
            }

            return new DatasetTablesResponseDto
            {
                DatasetId = datasetId.ToString(),
                DatasetName = dataset.Name,
                WorkspaceId = workspaceId.ToString(),
                WorkspaceName = workspaceName,
                MetadataSource = "executeQueries INFO functions (full schema)",
                Tables = tables
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // STRATEGY B – REST datasources fallback
        // Always available with Dataset.Read.All + workspace membership
        // ─────────────────────────────────────────────────────────────────────

        private async Task<DatasetTablesResponseDto> GetTablesViaRestFallbackAsync(
            Guid workspaceId,
            Guid datasetId)
        {
            var pbiClient = await GetPowerBIClientAsync();
            var dataset = await pbiClient.Datasets.GetDatasetInGroupAsync(
                workspaceId, datasetId.ToString());

            string workspaceName = await GetWorkspaceNameAsync(pbiClient, workspaceId);

            var datasourcesResponse = await pbiClient.Datasets
                .GetDatasourcesInGroupAsync(workspaceId, datasetId.ToString());

            var tables = new List<DatasetTableDto>();

            if (datasourcesResponse?.Value != null)
            {
                foreach (var ds in datasourcesResponse.Value)
                {
                    try
                    {
                        var connJson = JsonConvert.SerializeObject(ds.ConnectionDetails);
                        var connObj = JObject.Parse(connJson);

                        string? dbName = connObj["database"]?.ToString();
                        string? server = connObj["server"]?.ToString();
                        string? path = connObj["path"]?.ToString();
                        string? url = connObj["url"]?.ToString();

                        string tableName = !string.IsNullOrWhiteSpace(dbName)
                            ? dbName
                            : !string.IsNullOrWhiteSpace(path)
                                ? System.IO.Path.GetFileName(path)
                                : !string.IsNullOrWhiteSpace(url)
                                    ? url
                                    : ds.DatasourceType ?? "Unknown Source";

                        string description = !string.IsNullOrWhiteSpace(server)
                            ? $"Server: {server} | Type: {ds.DatasourceType}"
                            : $"Type: {ds.DatasourceType}";

                        if (!tables.Any(t =>
                            t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
                        {
                            tables.Add(new DatasetTableDto
                            {
                                TableName = tableName,
                                Description = description,
                                Columns = new List<DatasetColumnDto>(),
                                Measures = new List<DatasetMeasureDto>()
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            "Could not parse datasource connection details: {Msg}", ex.Message);
                    }
                }
            }

            return new DatasetTablesResponseDto
            {
                DatasetId = datasetId.ToString(),
                DatasetName = dataset.Name,
                WorkspaceId = workspaceId.ToString(),
                WorkspaceName = workspaceName,
                MetadataSource = tables.Any()
                    ? "REST API – datasource connection details (limited metadata)"
                    : "REST API – no connection details available",
                Tables = tables
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private async Task<string> GetWorkspaceNameAsync(PowerBIClient pbiClient, Guid workspaceId)
        {
            try
            {
                var groups = await pbiClient.Groups.GetGroupsAsync();
                return groups.Value
                    .FirstOrDefault(g => g.Id == workspaceId)?.Name
                    ?? workspaceId.ToString();
            }
            catch
            {
                return workspaceId.ToString();
            }
        }

        /// <summary>
        /// Runs a single DAX INFO query via executeQueries and returns parsed rows.
        /// </summary>
        private async Task<List<Dictionary<string, string>>> RunInfoQueryAsync(
            string bearerToken,
            string executeQueriesUrl,
            string daxQuery)
        {
            var body = JsonConvert.SerializeObject(new
            {
                queries = new[] { new { query = daxQuery } },
                serializerSettings = new { includeNulls = true }
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, executeQueriesUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "executeQueries failed. Status={Status}, Body={Body}",
                    response.StatusCode, responseBody);

                throw new HttpRequestException(
                    $"executeQueries failed ({response.StatusCode}): {responseBody}");
            }

            return ParseDaxRows(responseBody);
        }

        /// <summary>
        /// Parses the Power BI executeQueries JSON response into row dictionaries.
        /// Response shape:
        /// { "results": [ { "tables": [ { "rows": [ { "[col]": val } ] } ] } ] }
        /// </summary>
        private static List<Dictionary<string, string>> ParseDaxRows(string json)
        {
            var result = new List<Dictionary<string, string>>();

            try
            {
                var root = JObject.Parse(json);
                var rows = root["results"]?[0]?["tables"]?[0]?["rows"];

                if (rows == null) return result;

                foreach (var row in rows)
                {
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var prop in ((JObject)row).Properties())
                    {
                        // INFO functions prefix column names with the table alias e.g.
                        // "[TableName]" → strip everything up to and including "]"
                        string key = prop.Name.Contains(']')
                            ? prop.Name[(prop.Name.LastIndexOf(']') + 1)..].Trim()
                            : prop.Name.Trim();

                        dict[key] = prop.Value?.ToString() ?? string.Empty;
                    }
                    result.Add(dict);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ParseDaxRows warning: {ex.Message}");
            }

            return result;
        }

        /// <summary>Maps Power BI numeric data-type codes to human-readable strings.</summary>
        private static string MapDataType(string raw) => raw switch
        {
            "2" => "Text",
            "6" => "Whole Number",
            "8" => "Decimal Number",
            "9" => "Date/Time",
            "10" => "Date",
            "11" => "Time",
            "17" => "True/False (Boolean)",
            "20" => "Binary",
            _ => string.IsNullOrWhiteSpace(raw) ? "Unknown" : raw
        };

        public async Task<List<string>> GetLakehouseNamesAsync(Guid workspaceId)
        {
            string accessToken = await _aadService.GetAccessToken();
            // Note: Fabric API uses api.fabric.microsoft.com
            string url = $"https://api.fabric.microsoft.com/v1/workspaces/{workspaceId}/lakehouses";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Fabric API failed: {responseBody}");
            }

            var json = JObject.Parse(responseBody);
            var names = json["value"]?
                .Select(lh => lh["displayName"]?.ToString())
                .Where(name => name != null)
                .ToList();

            return names ?? new List<string>();
        }
    }
}