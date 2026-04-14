using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.AI.Projects.Agents;
using Azure.Core;
using Azure.Identity;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    // ✅ Response model for LLM server
    internal class LlmServerResponse
    {
        [JsonPropertyName("conversationId")]
        public string? ConversationId { get; set; }

        [JsonPropertyName("question")]
        public string? Question { get; set; }

        [JsonPropertyName("output_text")]
        public string? OutputText { get; set; }

        [JsonPropertyName("source")]
        public object? Source { get; set; }
    }

    public class AiAgentService : IAiAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly MDMDbContext _db;
        private readonly AzureAdSettings _azureAd;
        private readonly LakehouseTableService _lakehouseTableService;
        
        // ✅ AI Foundry config fields
        private readonly string _projectEndpoint;
        private readonly string _agentName;
        private readonly string _agentVersion;
        // ✅ Step 1 — Map LakehouseConfigDto → LakehouseConfigWithTypeDto
        private LakehouseConfigWithTypeDto MapToConfigWithType(LakehouseConfigDto config)
        {
            return new LakehouseConfigWithTypeDto
            {
                Lakehouse = config.Lakehouse,
                Tables = config.Tables.Select(t => new TableConfig
                {
                    TableName = t.TableName,
                    Columns = t.Columns,
                    ColumnTypes = new Dictionary<string, string>() // populated by BuildSchemaContextAsync
                }).ToList()
            };
        }
        public AiAgentService(
            IConfiguration config,
            MDMDbContext db,
            IOptions<AzureAdSettings> azureAdOptions,
            LakehouseTableService lakehouseTableService,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _db = db;
            _azureAd = azureAdOptions.Value;
            _lakehouseTableService = lakehouseTableService;
            
            // ✅ Load AI Foundry config from appsettings
            _projectEndpoint = config["Foundry:ProjectEndpoint"]
                ?? throw new InvalidOperationException("Foundry:ProjectEndpoint is missing in appsettings.");
            _agentName = config["Foundry:AgentName"] ?? "Agent3";
            _agentVersion = config["Foundry:AgentVersion"] ?? "3";
        }

        // ✅ Creates AIProjectClient using Service Principal
        // Same pattern as CreatePowerBIClientAsync()
        private AIProjectClient CreateAIProjectClient()
        {
            TokenCredential credential = new ClientSecretCredential(
                tenantId: _azureAd.TenantId,
                clientId: _azureAd.ClientId,
                clientSecret: _azureAd.ClientSecret
            );

            return new AIProjectClient(
                endpoint: new Uri(_projectEndpoint),
                tokenProvider: credential
            );
        }

        // ✅ Gets access token for Cognitive Services scope
        private async Task<string> GetAccessToken()
        {
            var app = ConfidentialClientApplicationBuilder
                .Create(_azureAd.ClientId)
                .WithClientSecret(_azureAd.ClientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{_azureAd.TenantId}")
                .Build();

            var result = await app.AcquireTokenForClient(
                new[] { "https://cognitiveservices.azure.com/.default" })
                .ExecuteAsync();

            return result.AccessToken;
        }

        // ✅ Main AI Foundry method — ask the agent a question
        public async Task<AiFoundryResponseDto> AskFoundryAgentAsync(AiFoundryRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserMessage))
            {
                return new AiFoundryResponseDto
                {
                    Success = false,
                    ErrorMessage = "User message cannot be empty."
                };
            }

            try
            {
                var projectClient = CreateAIProjectClient();

                var agentReference = new AgentReference(
                    name: _agentName,
                    version: _agentVersion
                );

                // Get the responses client scoped to this agent
                ProjectResponsesClient responseClient = projectClient.OpenAI
                    .GetProjectResponsesClientForAgent(agentReference);

                // Wrap sync SDK call to avoid blocking the thread pool
                var response = await Task.Run(() =>
                    responseClient.CreateResponse(dto.UserMessage)
                );

                var outputText = response.Value?.GetOutputText();

                return new AiFoundryResponseDto
                {
                    Output = outputText ?? string.Empty,
                    Success = true
                };
            }
            catch (Azure.RequestFailedException ex)
            {
                return new AiFoundryResponseDto
                {
                    Success = false,
                    ErrorMessage = $"Azure AI error ({ex.Status}): {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new AiFoundryResponseDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ✅ Lakehouse AI Query — full pipeline
        public async Task<AiQueryResponseDto> QueryAsync(AiQueryRequestDto dto)
        {
            try
            {
                // Step 1 — Resolve lakehouse config
                var lakehouseConfig = dto.LakehouseConfig;

                if (lakehouseConfig == null)
                {
                    var navItem = await _db.NavigationManagements
                        .FirstOrDefaultAsync(n => n.ReportId == dto.ReportId
                            && n.IsActive == true
                            && n.AiEnable == true);

                    if (navItem == null || string.IsNullOrEmpty(navItem.LakehouseConfig))
                        throw new ApplicationException($"No AI-enabled navigation found for ReportId '{dto.ReportId}'.");

                    lakehouseConfig = System.Text.Json.JsonSerializer.Deserialize<LakehouseConfigDto>(
                        navItem.LakehouseConfig,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                if (lakehouseConfig == null || lakehouseConfig.Tables == null || !lakehouseConfig.Tables.Any())
                    throw new ApplicationException("Lakehouse configuration is invalid or has no tables.");

                // Step 2 — Map to typed DTO (has ColumnTypes field)
                var typedConfig = MapToConfigWithType(lakehouseConfig);

                // Step 3 — Build schema context (also populates typedConfig.Tables[].ColumnTypes)
                var schemaContext = await BuildSchemaContextAsync(typedConfig);

                // Step 4 — Build dynamic rules from populated ColumnTypes
                var dynamicRules = BuildDynamicRules(typedConfig);

                // Step 5 — Generate SQL
                var generatedSql = await GenerateSqlFromQueryAsync(dto.UserQuery, schemaContext, dynamicRules, typedConfig);

                // Step 6 — Execute SQL
                var data = await _lakehouseTableService.ExecuteRawQueryAsync(
                    typedConfig.Lakehouse, generatedSql);

                // Step 7 — Natural language answer
                var answer = await GenerateNaturalAnswerAsync(dto.UserQuery, data);

                return new AiQueryResponseDto
                {
                    ReportId = dto.ReportId,
                    UserQuery = dto.UserQuery,
                    SqlGenerated = generatedSql,
                    Data = data,
                    Answer = answer,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new AiQueryResponseDto
                {
                    ReportId = dto.ReportId,
                    UserQuery = dto.UserQuery,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        // ✅ Builds schema context string for the LLM prompt
        public async Task<string> BuildSchemaContextAsync(LakehouseConfigWithTypeDto config)
        {
            var columnTypesPerTable = await _lakehouseTableService.GetColumnTypesAsync(config);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Lakehouse: {config.Lakehouse}");
            sb.AppendLine();
            sb.AppendLine("Schema:");
            sb.AppendLine(new string('-', 40));

            foreach (var table in config.Tables)
            {
                sb.AppendLine($"Table: {table.TableName}");
                sb.AppendLine("  Columns:");

                if (columnTypesPerTable.TryGetValue(table.TableName, out var columnTypes))
                {
                    // ✅ Populate ColumnTypes on the DTO for later use in BuildDynamicRules
                    table.ColumnTypes = columnTypes;

                    foreach (var column in table.Columns)
                    {
                        string dataType = columnTypes.TryGetValue(column, out var type) ? type : "unknown";
                        sb.AppendLine($"    - {column} ({dataType})");
                    }
                }
                else
                {
                    foreach (var column in table.Columns)
                        sb.AppendLine($"    - {column} (unknown)");
                }

                sb.AppendLine();
            }

            sb.AppendLine(new string('-', 40));
            return sb.ToString();
        }
        // ✅ Strips markdown code fences: ```sql ... ``` or ``` ... ```
        private string CleanLlmOutput(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var cleaned = raw.Trim();

            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"^```[a-zA-Z]*\s*",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"```$",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return cleaned.Trim();
        }

        // ✅ Builds SQL generation prompt and calls LLM
        private async Task<string> GenerateSqlFromQueryAsync(
    string userQuery,
    string schemaContext,
    string dynamicRules,
    LakehouseConfigWithTypeDto config)   // ← now typed DTO
        {
            var prompt = $"""
        You are a SQL expert for Microsoft Fabric Lakehouse (T-SQL dialect).

        ## Schema
        {schemaContext}

        ## Strict Rules
        1. Use ONLY tables and columns listed in the schema above. Never invent or assume columns.
        2. Use ONLY plain table names with square brackets.
           ✅ Correct : [{config.Tables.First().TableName}]
           ❌ Wrong   : [{config.Lakehouse}].[dbo].[{config.Tables.First().TableName}]
        3. Always wrap column and table names in square brackets [].
        4. Return ONLY the raw SQL query — no explanation, no markdown, no code blocks.

        ## Column Type Rules
        {dynamicRules}

        ## User Question
        {userQuery}

        ## Output
        Write a single valid T-SQL SELECT query that answers the user question strictly following the rules above.
        """;

            var response = await AskFoundryAgentAsync(new AiFoundryRequestDto
            {
                UserMessage = prompt
            });

            if (string.IsNullOrWhiteSpace(response.Output))
                throw new ApplicationException("LLM server returned an empty SQL response.");

            string cleanSql = StripDatabasePrefix(response.Output, config.Lakehouse);

            return cleanSql;
        }

        private string BuildDynamicRules(LakehouseConfigWithTypeDto config)  // ← now typed DTO
        {
            var sb = new System.Text.StringBuilder();
            int ruleNumber = 1;

            foreach (var table in config.Tables)
            {
                if (table.ColumnTypes == null || !table.ColumnTypes.Any())
                    continue;

                var datetimeColumns = table.ColumnTypes
                    .Where(c => c.Value.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("time", StringComparison.OrdinalIgnoreCase))
                    .Select(c => $"[{c.Key}]").ToList();

                if (datetimeColumns.Any())
                {
                    sb.AppendLine($"{ruleNumber++}. In table [{table.TableName}], " +
                                  $"datetime columns are: {string.Join(", ", datetimeColumns)}");
                    sb.AppendLine($"   → Always use FORMAT(column, 'yyyy-MM') in SELECT and GROUP BY.");
                }

                var numericColumns = table.ColumnTypes
                    .Where(c => c.Value.Contains("decimal", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("numeric", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("float", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("int", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("money", StringComparison.OrdinalIgnoreCase))
                    .Select(c => $"[{c.Key}]").ToList();

                if (numericColumns.Any())
                {
                    sb.AppendLine($"{ruleNumber++}. In table [{table.TableName}], " +
                                  $"numeric columns are: {string.Join(", ", numericColumns)}");
                    sb.AppendLine($"   → Always wrap with SUM(), AVG(), or COUNT(). Never GROUP BY or WHERE on these.");
                }

                var stringColumns = table.ColumnTypes
                    .Where(c => c.Value.Contains("varchar", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("char", StringComparison.OrdinalIgnoreCase) ||
                                c.Value.Contains("text", StringComparison.OrdinalIgnoreCase))
                    .Select(c => $"[{c.Key}]").ToList();

                if (stringColumns.Any())
                {
                    sb.AppendLine($"{ruleNumber++}. In table [{table.TableName}], " +
                                  $"string columns are: {string.Join(", ", stringColumns)}");
                    sb.AppendLine($"   → Can be used freely in SELECT, GROUP BY, and WHERE.");
                }
            }

            return sb.ToString();
        }

        private string StripDatabasePrefix(string sql, string lakehouseName)
        {
            if (string.IsNullOrWhiteSpace(sql))
                throw new Exception("SQL is empty.");

            sql = sql.Replace("```sql", "", StringComparison.OrdinalIgnoreCase)
                     .Replace("```", "")
                     .Trim();

            // Remove trailing semicolon from AI output
            sql = sql.TrimEnd().TrimEnd(';').Trim();

            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                $@"\[?{System.Text.RegularExpressions.Regex.Escape(lakehouseName)}\]?\.\[?dbo\]?\.?",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\[?dbo\]?\.?",
                "",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            string AddDynamicSchema(string tableName)
            {
                var clean = tableName.Trim('[', ']');
                var parts = clean.Split('_', StringSplitOptions.RemoveEmptyEntries);
                var schema = parts.Length > 0 ? parts[0].ToLower() : "dbo";
                return $"[{schema}].[{clean}]";
            }

            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\b(FROM|JOIN)\s+\[(?![^\]]+\]\.\[)([^\]]+)\]",
                m => $"{m.Groups[1].Value} {AddDynamicSchema(m.Groups[2].Value)}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            sql = System.Text.RegularExpressions.Regex.Replace(
                sql,
                @"\b(FROM|JOIN)\s+(?!\[?[A-Za-z0-9_]+\]?\.)([A-Za-z_][A-Za-z0-9_]*)",
                m => $"{m.Groups[1].Value} {AddDynamicSchema(m.Groups[2].Value)}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            sql = sql.Trim();

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                sql,
                @"^(SELECT|WITH)\s",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                throw new Exception("Only SELECT queries are allowed.");
            }

            return sql;
        }
        // ✅ Summarizes query results using LLM
        private async Task<string> GenerateNaturalAnswerAsync(
            string userQuery,
            List<Dictionary<string, object?>> data)
        {
            if (data == null || !data.Any())
                return "No data was found for your query.";

            var dataSummary = System.Text.Json.JsonSerializer.Serialize(data.Take(10));

            var prompt = $"The user asked: \"{userQuery}\". " +
                         $"Here is the query result (up to 10 rows): {dataSummary}. " +
                         $"Write a concise friendly natural language answer summarizing the data.";

            var answer = await AskFoundryAgentAsync(new AiFoundryRequestDto
            {
                UserMessage = prompt
            });

            return string.IsNullOrWhiteSpace(answer.Output)
                ? "Data was retrieved but could not be summarized."
                : answer.Output;
        }
    }
}