using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class XmlaSemanticService
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly AzureAdSettings _azureAd;


        private static readonly HttpClient _http = new HttpClient();

        // Inject these via IConfiguration or appsettings.json
        public XmlaSemanticService(IOptions<AzureAdSettings> azureAdOptions)
        {
            _azureAd = azureAdOptions.Value;
            _tenantId = _azureAd.TenantId;
            _clientId = _azureAd.ClientId;
            _clientSecret = _azureAd.ClientSecret;
        }

        public async Task<object> GetFullSchemaAsync(string workspaceId, string datasetId)
        {
            try
            {
                var token = await GetAccessTokenAsync();

                // -----------------------------------
                // TABLES + COLUMNS + MEASURES
                // -----------------------------------
                var tables = await GetTablesAsync(token, workspaceId, datasetId);

                // -----------------------------------
                // RELATIONSHIPS
                // -----------------------------------
                var relationships = await GetRelationshipsAsync(token, workspaceId, datasetId);

                // -----------------------------------
                // AI FRIENDLY TEXT FORMAT
                // -----------------------------------
                var schemaText = BuildSchemaText(tables, relationships);

                return new
                {
                    datasetId,
                    tables,
                    relationships,
                    schemaText
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Schema extraction failed: " + ex.Message, ex);
            }
        }

        // -----------------------------------
        // GET TABLES + COLUMNS + MEASURES
        // -----------------------------------
        private async Task<List<TableSchema>> GetTablesAsync(string token, string workspaceId, string datasetId)
        {
            // Fetch tables
            var tablesUrl = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/tables";
            var tablesJson = await CallApiAsync(token, tablesUrl);

            var tablesList = new List<TableSchema>();

            if (tablesJson.TryGetProperty("value", out var tablesArray))
            {
                foreach (var t in tablesArray.EnumerateArray())
                {
                    var tableName = t.GetProperty("name").GetString();

                    // Fetch columns for this table
                    var columnsUrl = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/tables/{tableName}/columns";

                    var columns = new List<ColumnSchema>();
                    var measures = new List<MeasureSchema>();

                    try
                    {
                        var colJson = await CallApiAsync(token, columnsUrl);
                        if (colJson.TryGetProperty("value", out var colArray))
                        {
                            foreach (var c in colArray.EnumerateArray())
                            {
                                var colType = c.TryGetProperty("columnType", out var ct) ? ct.GetString() : "regular";

                                if (colType == "measure")
                                {
                                    measures.Add(new MeasureSchema
                                    {
                                        Name = c.GetProperty("name").GetString(),
                                        Expression = c.TryGetProperty("expression", out var expr)
                                            ? expr.GetString() : "",
                                        FormatString = c.TryGetProperty("formatString", out var fmt)
                                            ? fmt.GetString() : ""
                                    });
                                }
                                else
                                {
                                    columns.Add(new ColumnSchema
                                    {
                                        Name = c.GetProperty("name").GetString(),
                                        DataType = c.TryGetProperty("dataType", out var dt)
                                            ? dt.GetString() : "unknown",
                                        IsHidden = c.TryGetProperty("isHidden", out var ih)
                                            && ih.GetBoolean()
                                    });
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Some tables may not expose columns via REST — skip gracefully
                    }

                    tablesList.Add(new TableSchema
                    {
                        TableName = tableName,
                        Columns = columns,
                        Measures = measures
                    });
                }
            }

            return tablesList;
        }

        // -----------------------------------
        // GET RELATIONSHIPS
        // -----------------------------------
        private async Task<List<RelationshipSchema>> GetRelationshipsAsync(string token, string workspaceId, string datasetId)
        {
            var url = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets/{datasetId}/relationships";
            var json = await CallApiAsync(token, url);

            var result = new List<RelationshipSchema>();

            if (json.TryGetProperty("value", out var arr))
            {
                foreach (var r in arr.EnumerateArray())
                {
                    result.Add(new RelationshipSchema
                    {
                        FromTable = r.TryGetProperty("fromTable", out var ft) ? ft.GetString() : "",
                        FromColumn = r.TryGetProperty("fromColumn", out var fc) ? fc.GetString() : "",
                        ToTable = r.TryGetProperty("toTable", out var tt) ? tt.GetString() : "",
                        ToColumn = r.TryGetProperty("toColumn", out var tc) ? tc.GetString() : "",
                        CrossFilteringBehavior = r.TryGetProperty("crossFilteringBehavior", out var cf)
                            ? cf.GetString() : "OneDirection",
                        IsActive = !r.TryGetProperty("isActive", out var ia) || ia.GetBoolean()
                    });
                }
            }

            return result;
        }

        // -----------------------------------
        // SHARED HTTP HELPER
        // -----------------------------------
        private async Task<JsonElement> CallApiAsync(string token, string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body).RootElement;
        }

        // -----------------------------------
        // AZURE AD TOKEN (Client Credentials)
        // -----------------------------------
        private async Task<string> GetAccessTokenAsync()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(_clientId)
                .WithClientSecret(_clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{_tenantId}")
                .Build();

            var result = await app
                .AcquireTokenForClient(new[] { "https://analysis.windows.net/powerbi/api/.default" })
                .ExecuteAsync();

            return result.AccessToken;
        }

        // -----------------------------------
        // AI FRIENDLY TEXT FORMAT
        // -----------------------------------
        private string BuildSchemaText(List<TableSchema> tables, List<RelationshipSchema> relationships)
        {
            var text = "Dataset Schema:\n\n";

            foreach (var table in tables)
            {
                text += $"Table: {table.TableName}\n";
                foreach (var col in table.Columns)
                    text += $"  - {col.Name} ({col.DataType})\n";
                foreach (var measure in table.Measures)
                    text += $"  * Measure: {measure.Name}\n";
                text += "\n";
            }

            text += "Relationships:\n";
            foreach (var rel in relationships)
                text += $"  {rel.FromTable}.{rel.FromColumn} -> {rel.ToTable}.{rel.ToColumn}\n";

            return text;
        }
    }
}