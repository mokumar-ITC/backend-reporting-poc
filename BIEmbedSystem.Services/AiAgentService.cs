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
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class AiAgentService : IAiAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly MDMDbContext _db;
        private readonly AzureAdSettings _azureAd;
        private readonly LakehouseTableService _lakehouseTableService;
        private readonly IHttpClientFactory _httpClientFactory;
        

        public AiAgentService(
            HttpClient httpClient, 
            IConfiguration config, 
            MDMDbContext db, 
            IOptions<AzureAdSettings> azureAdOptions, 
            LakehouseTableService lakehouseTableService,
            IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClient;
            _config = config;
            _db = db;
            _azureAd = azureAdOptions.Value; ;
            _lakehouseTableService = lakehouseTableService;
            _httpClientFactory = httpClientFactory;
        }

        private async Task<string> GetAccessToken()
        {
            var tenantId = _azureAd.TenantId;
            var clientId = _azureAd.ClientId;
            var clientSecret = _azureAd.ClientSecret;

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority($"https://login.microsoftonline.com/{tenantId}")
                .Build();

            var result = await app.AcquireTokenForClient(
                new[] { "https://cognitiveservices.azure.com/.default" })
                .ExecuteAsync();

            return result.AccessToken;
        }

        // 🚀 CREATE AGENT (MULTIPLE SEMANTIC MODELS)
        //public async Task<string> CreateAgentAsync(CreateAgentDto dto)
        //{
        //    var token = await GetAccessToken();

        //    var url = $"{_config["Foundry:ProjectEndpoint"]}/agents";

        //    var requestBody = new
        //    {
        //        name = dto.AgentName,
        //        instructions = "Fabric AI Agent with multiple semantic models",
        //        tools = new[]
        //        {
        //        new
        //        {
        //            type = "fabric_data_agent",
        //            fabricAgentId = dto.FabricAgentId,
        //            semanticModelIds = dto.SemanticModelIds   // ✅ IMPORTANT CHANGE
        //        }
        //    }
        //    };

        //    var request = new HttpRequestMessage(HttpMethod.Post, url);
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //    request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(request);
        //    var content = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception(content);

        //    dynamic json = JsonConvert.DeserializeObject(content);
        //    string foundryAgentId = json.id;

        //    // 💾 Save Parent
        //    var agent = new AiAgent
        //    {
        //        Id = Guid.NewGuid(),
        //        AgentName = dto.AgentName,
        //        FoundryAgentId = foundryAgentId,
        //        FabricAgentId = dto.FabricAgentId,
        //        WorkspaceId = dto.WorkspaceId,
        //        CreatedAt = DateTime.UtcNow
        //    };

        //    _db.AiAgents.Add(agent);

        //    // 💾 Save Child (Multiple Semantic Models)
        //    foreach (var modelId in dto.SemanticModelIds)
        //    {
        //        _db.AiAgentSemanticModels.Add(new AiAgentSemanticModel
        //        {
        //            Id = Guid.NewGuid(),
        //            AgentId = agent.Id,
        //            SemanticModelId = modelId
        //        });
        //    }

        //    await _db.SaveChangesAsync();

        //    return foundryAgentId;
        //}

        // ✏️ UPDATE AGENT
        //public async Task<string> UpdateAgentAsync(UpdateAgentDto dto)
        //{
        //    var agent = await _db.AiAgents
        //        .Include(a => a.SemanticModels)
        //        .FirstOrDefaultAsync(a => a.Id == dto.Id);

        //    if (agent == null)
        //        throw new Exception("Agent not found");

        //    var token = await GetAccessToken();

        //    var url = $"{_config["Foundry:ProjectEndpoint"]}/agents/{agent.FoundryAgentId}";

        //    var requestBody = new
        //    {
        //        name = dto.AgentName,
        //        tools = new[]
        //        {
        //        new
        //        {
        //            type = "fabric_data_agent",
        //            semanticModelIds = dto.SemanticModelIds
        //        }
        //    }
        //    };

        //    var request = new HttpRequestMessage(HttpMethod.Patch, url);
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        //    request.Content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        //    var response = await _httpClient.SendAsync(request);
        //    var content = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //        throw new Exception(content);

        //    // 💾 Update DB

        //    agent.AgentName = dto.AgentName;
        //    agent.UpdatedAt = DateTime.UtcNow;

        //    // Remove old mappings
        //    _db.AiAgentSemanticModels.RemoveRange(agent.SemanticModels);

        //    // Add new mappings
        //    foreach (var modelId in dto.SemanticModelIds)
        //    {
        //        _db.AiAgentSemanticModels.Add(new AiAgentSemanticModel
        //        {
        //            Id = Guid.NewGuid(),
        //            AgentId = agent.Id,
        //            SemanticModelId = modelId
        //        });
        //    }

        //    await _db.SaveChangesAsync();

        //    return "Updated successfully";
        //}

        //public async Task<CheckAgentResponse> CheckAgentAsync(CheckAgentDto dto)
        //{
        //    //check find the agent present with semnatic modelId and workpaceId
        //    AiAgentSemanticModel agent = await _db.AiAgentSemanticModels.Where(a => a.SemanticModelId == dto.SemanticModelId).FirstOrDefaultAsync();
        //    if (agent == null)
        //    {
        //        return new CheckAgentResponse
        //        {
        //            Success = false
        //        };
        //    }
        //    else
        //    {
        //        return new CheckAgentResponse
        //        {
        //            Success = true,
        //            AgentId = agent.AgentId,
        //        };
        //    }
            
        //}

        // ❌ DELETE AGENT
        //public async Task DeleteAgentAsync(Guid id)
        //{
        //    var agent = await _db.AiAgents
        //        .Include(a => a.SemanticModels)
        //        .FirstOrDefaultAsync(a => a.Id == id);

        //    if (agent == null)
        //        throw new Exception("Agent not found");

        //    var token = await GetAccessToken();

        //    var url = $"{_config["Foundry:ProjectEndpoint"]}/agents/{agent.FoundryAgentId}";

        //    var request = new HttpRequestMessage(HttpMethod.Delete, url);
        //    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        //    var response = await _httpClient.SendAsync(request);

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        var content = await response.Content.ReadAsStringAsync();
        //        throw new Exception(content);
        //    }

        //    // 💾 Delete DB data
        //    _db.AiAgentSemanticModels.RemoveRange(agent.SemanticModels);
        //    _db.AiAgents.Remove(agent);

        //    await _db.SaveChangesAsync();
        //}

        public async Task<AiQueryResponseDto> QueryAsync(AiQueryRequestDto dto)
        {
            
            try
            {
                // Step 1 — Resolve lakehouse config from request or look up via reportId
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

                // Step 2 — Build schema context string for AI prompt
                var schemaContext = await BuildSchemaContextAsync(lakehouseConfig);

                // Step 3 — Generate SQL from user query using AI
                var generatedSql = await GenerateSqlFromQueryAsync(dto.UserQuery, schemaContext, lakehouseConfig);

                // Step 4 — Execute SQL against lakehouse
                var data = await _lakehouseTableService.ExecuteRawQueryAsync(
                    lakehouseConfig.Lakehouse, generatedSql);

                // Step 5 — Build natural language answer
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

        // ✅ Builds schema string like:
        // Lakehouse: lh_acc_dev_gold
        // Table: sla_dim → columns: sla_id, sla_name, sla_target
        public async Task<string> BuildSchemaContextAsync(LakehouseConfigDto config)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Lakehouse: {config.Lakehouse}");

            foreach (var table in config.Tables)
            {
                sb.AppendLine($"Table: {table.TableName} → columns: {string.Join(", ", table.Columns)}");
            }

            return await Task.FromResult(sb.ToString());
        }

        // ✅ Calls external LLM server: GET http://localhost:3001/ask?q=your+query
        private async Task<string> CallLlmServerAsync(string query)
        {
            if (_httpClientFactory == null)
                throw new ApplicationException("HttpClientFactory is null — DI injection failed.");

            var client = _httpClientFactory.CreateClient("LlmServer");
            var encodedQuery = Uri.EscapeDataString(query);
            var url = $"/ask?q={encodedQuery}";

            
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new ApplicationException($"LLM server error ({response.StatusCode}): {errorBody}");
            }

            var raw = await response.Content.ReadAsStringAsync();
           
            // ✅ Parse the JSON response
            if (raw.TrimStart().StartsWith("{"))
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<LlmServerResponse>(raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (parsed?.OutputText != null)
                    return CleanLlmOutput(parsed.OutputText);
            }

            return CleanLlmOutput(raw);
        }

        // ✅ Strips markdown code fences like ```sql ... ``` or ``` ... ```
        private string CleanLlmOutput(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var cleaned = raw.Trim();

            // Remove ```sql or ``` at start
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"^```[a-zA-Z]*\s*",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            // Remove ``` at end
            cleaned = System.Text.RegularExpressions.Regex.Replace(
                cleaned,
                @"```$",
                string.Empty,
                System.Text.RegularExpressions.RegexOptions.Multiline);

            return cleaned.Trim();
        }
        // ✅ Builds prompt with schema context and sends to LLM server
        private async Task<string> GenerateSqlFromQueryAsync(
            string userQuery,
            string schemaContext,
            LakehouseConfigDto config)
        {
            var prompt = $"You are a SQL expert for Microsoft Fabric Lakehouse. " +
                         $"Available schema: {schemaContext}. " +
                         $"User question: {userQuery}. " +
                         $"Write a valid T-SQL SELECT query using only the tables and columns listed. " +
                         $"Only return the SQL query, no explanation.";

            var sql = await CallLlmServerAsync(prompt);

            if (string.IsNullOrWhiteSpace(sql))
                throw new ApplicationException("LLM server returned an empty SQL response.");

            // Safety check — only allow SELECT
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                throw new ApplicationException("AI returned a non-SELECT query. Rejected for safety.");

            return sql.Trim();
        }


        // ✅ Sends data back to LLM server for natural language summary
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

            var answer = await CallLlmServerAsync(prompt);

            return string.IsNullOrWhiteSpace(answer)
                ? "Data was retrieved but could not be summarized."
                : answer;
        }
    }
}
