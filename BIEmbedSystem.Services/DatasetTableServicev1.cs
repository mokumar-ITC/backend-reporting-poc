using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.PowerBI.Api;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace BIEmbedSystem.Services
{
    public class DatasetTableServicev1
    {
        private readonly AadService _aadService;
        private readonly IOptions<AzureAdSettings> _azureAd;
        private readonly ILogger<DatasetTableServicev1> _logger;
        private readonly HttpClient _httpClient;

        private readonly string _powerBiApiUrl = "https://api.powerbi.com";

        public DatasetTableServicev1(
            AadService aadService,
            IOptions<AzureAdSettings> azureAd,
            ILogger<DatasetTableServicev1> logger,
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

        /// <summary>Creates a Power BI SDK client using the delegated token from AadService.</summary>
        private async Task<PowerBIClient> GetPowerBIClientAsync()
        {
            var accessToken = await _aadService.GetEmbedTokenAsync();
            var tokenCredentials = new TokenCredentials(accessToken, "Bearer");
            return new PowerBIClient(new Uri(_powerBiApiUrl), tokenCredentials);
        }

        /// <summary>
        /// Executes a DAX query against a dataset using the executeQueries API.
        /// Uses the delegated token — requires Dataset.Read.All permission.
        /// Works on Pro license (no Premium or Admin API needed).
        /// </summary>
        private async Task<JObject> ExecuteDatasetQueryAsync(
            string accessToken,
            Guid workspaceId,
            Guid datasetId,
            string daxQuery)
        {
            string url = $"{_powerBiApiUrl}/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/executeQueries";

            var body = JsonConvert.SerializeObject(new
            {
                queries = new[]
                {
                    new { query = daxQuery }
                },
                serializerSettings = new
                {
                    includeNulls = true
                }
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "[ExecuteQuery] Failed → HTTP {Status} | Query={Query} | Body={Body}",
                    (int)response.StatusCode, daxQuery, responseBody);

                throw new HttpRequestException(
                    $"executeQueries API failed ({response.ReasonPhrase}): {responseBody}");
            }

            return JObject.Parse(responseBody);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 1.  GET TABLES FOR A SPECIFIC DATASET
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all tables with columns and measures for a dataset.
        /// Uses executeQueries (DAX INFO functions) — works with Pro license.
        /// Requires Dataset.Read.All (delegated).
        /// </summary>
        public async Task<DatasetTablesResponseDto> GetTablesForDatasetAsync(
            Guid workspaceId,
            Guid datasetId)
        {
            _logger.LogInformation(
                "GetTablesForDataset → workspaceId={WorkspaceId}, datasetId={DatasetId}",
                workspaceId, datasetId);

            var accessToken = await _aadService.GetEmbedTokenAsync();

            // ── Step 1: Get all tables ────────────────────────────────────────
            var tablesResult = await ExecuteDatasetQueryAsync(
                accessToken, workspaceId, datasetId,
                "EVALUATE SELECTCOLUMNS(INFO.TABLES(), " +
                "\"TableID\", [ID], " +
                "\"TableName\", [Name], " +
                "\"IsHidden\", [IsHidden], " +
                "\"Description\", [Description])");

            var tables = ParseQueryRows(tablesResult);

            // ── Step 2: Get all columns ───────────────────────────────────────
            var columnsResult = await ExecuteDatasetQueryAsync(
                accessToken, workspaceId, datasetId,
                "EVALUATE SELECTCOLUMNS(INFO.COLUMNS(), " +
                "\"TableID\", [TableID], " +
                "\"ColumnName\", [ExplicitName], " +
                "\"DataType\", [ExplicitDataType], " +
                "\"IsHidden\", [IsHidden])");

            var columns = ParseQueryRows(columnsResult);

            // ── Step 3: Get all measures ──────────────────────────────────────
            var measuresResult = await ExecuteDatasetQueryAsync(
                accessToken, workspaceId, datasetId,
                "EVALUATE SELECTCOLUMNS(INFO.MEASURES(), " +
                "\"TableID\", [TableID], " +
                "\"MeasureName\", [Name], " +
                "\"Expression\", [Expression], " +
                "\"IsHidden\", [IsHidden], " +
                "\"Description\", [Description])");

            var measures = ParseQueryRows(measuresResult);

            // ── Step 4: Map to DTO ────────────────────────────────────────────
            return MapToDto(workspaceId.ToString(), datasetId.ToString(), tables, columns, measures);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 2.  GET TABLES FOR A REPORT  (resolves datasetId automatically)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the dataset that backs a report, then returns its tables.
        /// Requires Report.Read.All + Dataset.Read.All (delegated).
        /// </summary>
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
                    $"Report {reportId} does not have an associated dataset " +
                    "(it may be a paginated RDL report).");

            var datasetId = Guid.Parse(report.DatasetId);

            _logger.LogInformation(
                "Resolved datasetId={DatasetId} for reportId={ReportId}", datasetId, reportId);

            return await GetTablesForDatasetAsync(workspaceId, datasetId);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 3.  GET TABLE SUMMARY FOR ALL REPORTS IN A WORKSPACE
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns table name summary for every report in the workspace.
        /// Deduplicates by datasetId to avoid redundant executeQueries calls.
        /// </summary>
        public async Task<List<ReportDatasetTableSummaryDto>> GetTableSummaryForWorkspaceAsync(
            Guid workspaceId)
        {
            _logger.LogInformation(
                "GetTableSummaryForWorkspace → workspaceId={WorkspaceId}", workspaceId);

            var pbiClient = await GetPowerBIClientAsync();
            var accessToken = await _aadService.GetEmbedTokenAsync();

            var reportsResponse = await pbiClient.Reports.GetReportsInGroupAsync(workspaceId);
            var reports = reportsResponse.Value
                .Where(r => !string.IsNullOrWhiteSpace(r.DatasetId))
                .ToList();

            if (!reports.Any())
                return new List<ReportDatasetTableSummaryDto>();

            // ── Cache table names per datasetId to avoid duplicate API calls ──
            var datasetTableCache = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            var result = new List<ReportDatasetTableSummaryDto>();

            foreach (var report in reports)
            {
                List<string> tableNames;

                if (datasetTableCache.TryGetValue(report.DatasetId, out var cached))
                {
                    tableNames = cached;
                }
                else
                {
                    try
                    {
                        var datasetId = Guid.Parse(report.DatasetId);

                        var tablesResult = await ExecuteDatasetQueryAsync(
                            accessToken, workspaceId, datasetId,
                            "EVALUATE SELECTCOLUMNS(INFO.TABLES(), " +
                            "\"TableName\", [Name], " +
                            "\"IsHidden\", [IsHidden])");

                        tableNames = ParseQueryRows(tablesResult)
                            .Select(r => r["TableName"]?.ToString() ?? string.Empty)
                            .Where(n => !string.IsNullOrWhiteSpace(n))
                            .ToList();

                        datasetTableCache[report.DatasetId] = tableNames;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Could not retrieve tables for datasetId={DatasetId}, skipping.",
                            report.DatasetId);
                        tableNames = new List<string>();
                    }
                }

                result.Add(new ReportDatasetTableSummaryDto
                {
                    ReportId = report.Id.ToString(),
                    ReportName = report.Name,
                    DatasetId = report.DatasetId,
                    TableNames = tableNames
                });
            }

            return result;
        }

        // ─────────────────────────────────────────────────────────────────────
        // PARSE HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Parses the rows from an executeQueries response into a flat list of dictionaries.
        /// Response shape:
        /// {
        ///   "results": [{
        ///     "tables": [{
        ///       "rows": [{ "col1": val1, ... }, ...]
        ///     }]
        ///   }]
        /// }
        /// </summary>
        private static List<Dictionary<string, object?>> ParseQueryRows(JObject response)
        {
            var rows = response["results"]?[0]?["tables"]?[0]?["rows"];

            if (rows == null)
                return new List<Dictionary<string, object?>>();

            return rows
                .Select(row => row.ToObject<Dictionary<string, object?>>()
                               ?? new Dictionary<string, object?>())
                .ToList();
        }

        // ─────────────────────────────────────────────────────────────────────
        // MAPPING
        // ─────────────────────────────────────────────────────────────────────

        private DatasetTablesResponseDto MapToDto(
            string workspaceId,
            string datasetId,
            List<Dictionary<string, object?>> tables,
            List<Dictionary<string, object?>> columns,
            List<Dictionary<string, object?>> measures)
        {
            var tableDtos = new List<DatasetTableDto>();

            foreach (var table in tables)
            {
                var tableId = table["TableID"]?.ToString() ?? string.Empty;
                var tableName = table["TableName"]?.ToString() ?? string.Empty;

                // Match columns by TableID
                var tableCols = columns
                    .Where(c => c["TableID"]?.ToString() == tableId)
                    .Select(c => new DatasetColumnDto
                    {
                        ColumnName = c["ColumnName"]?.ToString() ?? string.Empty,
                        DataType = MapDataType(c["DataType"]?.ToString()),
                        IsHidden = c["IsHidden"] is bool b ? b
                                     : bool.TryParse(c["IsHidden"]?.ToString(), out var bv) && bv
                    })
                    .ToList();

                // Match measures by TableID
                var tableMeasures = measures
                    .Where(m => m["TableID"]?.ToString() == tableId)
                    .Select(m => new DatasetMeasureDto
                    {
                        MeasureName = m["MeasureName"]?.ToString() ?? string.Empty,
                        Expression = m["Expression"]?.ToString() ?? string.Empty,
                        IsHidden = m["IsHidden"] is bool mb ? mb
                                      : bool.TryParse(m["IsHidden"]?.ToString(), out var mbv) && mbv
                    })
                    .ToList();

                tableDtos.Add(new DatasetTableDto
                {
                    TableName = tableName,
                    Description = table["Description"]?.ToString() ?? string.Empty,
                    Columns = tableCols,
                    Measures = tableMeasures
                });
            }

            return new DatasetTablesResponseDto
            {
                DatasetId = datasetId,
                WorkspaceId = workspaceId,
                Tables = tableDtos
            };
        }

        /// <summary>
        /// Maps the numeric DataType value returned by INFO.COLUMNS()
        /// to a human-readable string.
        /// </summary>
        private static string MapDataType(string? dataTypeCode) => dataTypeCode switch
        {
            "2" => "Integer",
            "3" => "Decimal",
            "4" => "Float",
            "5" => "Currency",
            "6" => "Date",
            "7" => "Boolean",
            "8" => "Text",
            "9" => "Binary",
            "10" => "Unknown",
            "11" => "Variant",
            _ => dataTypeCode ?? "Unknown"
        };


    }
}