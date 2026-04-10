using Asp.Versioning;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Rest;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class DatasetTableController : ControllerBase
    {
        private readonly ILogger<DatasetTableController> _logger;
        private readonly IOptions<AzureAdSettings> _azureAd;
        private readonly DatasetTableService _datasetTableService;

        public DatasetTableController(
            ILogger<DatasetTableController> logger,
            IOptions<AzureAdSettings> azureAd,
            DatasetTableService datasetTableService)
        {
            _logger = logger;
            _azureAd = azureAd;
            _datasetTableService = datasetTableService;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/v1/datasettable/tables/dataset/{workspaceId}/{datasetId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all tables, columns, and measures for a specific dataset
        /// inside a Power BI workspace.
        /// Uses the Power BI Scanner (Admin) API with the service principal.
        /// </summary>
        /// <param name="workspaceId">Power BI workspace (group) GUID</param>
        /// <param name="datasetId">Power BI dataset (semantic model) GUID</param>
        [HttpGet("tables/dataset/{workspaceId}/{datasetId}")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(DatasetTablesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTablesByDatasetAsync(string workspaceId, string datasetId)
        {
            try
            {
                _logger.LogInformation(
                    "GetTablesByDataset → workspaceId={WorkspaceId}, datasetId={DatasetId}",
                    workspaceId, datasetId);

                if (!Guid.TryParse(workspaceId, out var wsGuid))
                    return BadRequest(new { message = "Invalid workspaceId format. Expected a GUID." });

                if (!Guid.TryParse(datasetId, out var dsGuid))
                    return BadRequest(new { message = "Invalid datasetId format. Expected a GUID." });

                var result = await _datasetTableService.GetTablesForDatasetAsync(wsGuid, dsGuid);
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                _logger.LogError(exc,
                    "Power BI API error in GetTablesByDataset. Status={Status}", exc.Response?.StatusCode);

                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format(
                    "Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode,
                    (int)exc.Response.StatusCode,
                    exc.Response.Content,
                    exc.Response.Headers["RequestId"]?.FirstOrDefault());

                return BadRequest(new { message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Dataset or workspace not found in scanner result.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetTablesByDataset.");
                HttpContext.Response.StatusCode = 500;
                return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/v1/datasettable/tables/report/{workspaceId}/{reportId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Resolves the dataset backing a report and returns all its tables,
        /// columns, and measures.
        /// </summary>
        /// <param name="workspaceId">Power BI workspace (group) GUID</param>
        /// <param name="reportId">Power BI report GUID</param>
        [HttpGet("tables/report/{workspaceId}/{reportId}")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(DatasetTablesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTablesByReportAsync(string workspaceId, string reportId)
        {
            try
            {
                _logger.LogInformation(
                    "GetTablesByReport → workspaceId={WorkspaceId}, reportId={ReportId}",
                    workspaceId, reportId);

                if (!Guid.TryParse(workspaceId, out var wsGuid))
                    return BadRequest(new { message = "Invalid workspaceId format. Expected a GUID." });

                if (!Guid.TryParse(reportId, out var rptGuid))
                    return BadRequest(new { message = "Invalid reportId format. Expected a GUID." });

                var result = await _datasetTableService.GetTablesForReportAsync(wsGuid, rptGuid);
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                _logger.LogError(exc,
                    "Power BI API error in GetTablesByReport. Status={Status}", exc.Response?.StatusCode);

                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format(
                    "Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode,
                    (int)exc.Response.StatusCode,
                    exc.Response.Content,
                    exc.Response.Headers["RequestId"]?.FirstOrDefault());

                return BadRequest(new { message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Report has no associated dataset (may be paginated).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetTablesByReport.");
                HttpContext.Response.StatusCode = 500;
                return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/v1/datasettable/tables/workspace/{workspaceId}/summary
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a lightweight summary of table names for every report
        /// in the given workspace. Useful for building a metadata catalogue.
        /// </summary>
        /// <param name="workspaceId">Power BI workspace (group) GUID</param>
        [HttpGet("tables/workspace/{workspaceId}/summary")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(List<ReportDatasetTableSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWorkspaceTableSummaryAsync(string workspaceId)
        {
            try
            {
                _logger.LogInformation(
                    "GetWorkspaceTableSummary → workspaceId={WorkspaceId}", workspaceId);

                if (!Guid.TryParse(workspaceId, out var wsGuid))
                    return BadRequest(new { message = "Invalid workspaceId format. Expected a GUID." });

                var result = await _datasetTableService.GetTableSummaryForWorkspaceAsync(wsGuid);
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                _logger.LogError(exc,
                    "Power BI API error in GetWorkspaceTableSummary. Status={Status}", exc.Response?.StatusCode);

                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format(
                    "Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode,
                    (int)exc.Response.StatusCode,
                    exc.Response.Content,
                    exc.Response.Headers["RequestId"]?.FirstOrDefault());

                return BadRequest(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetWorkspaceTableSummary.");
                HttpContext.Response.StatusCode = 500;
                return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET /api/v1/datasettable/tables/default-workspace/report/{reportId}
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Convenience endpoint: uses the WorkspaceId from AzureAdSettings (appsettings.json)
        /// so the caller does not need to pass it explicitly.
        /// </summary>
        /// <param name="reportId">Power BI report GUID</param>
        [HttpGet("tables/default-workspace/report/{reportId}")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(DatasetTablesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTablesByReportDefaultWorkspaceAsync(string reportId)
        {
            try
            {
                _logger.LogInformation(
                    "GetTablesByReportDefaultWorkspace → reportId={ReportId}", reportId);

                if (!Guid.TryParse(reportId, out var rptGuid))
                    return BadRequest(new { message = "Invalid reportId format. Expected a GUID." });

                if (!Guid.TryParse(_azureAd.Value.WorkspaceId, out var wsGuid))
                    return StatusCode(500, new
                    {
                        message = "Default WorkspaceId is not configured correctly in AzureAdSettings."
                    });

                var result = await _datasetTableService.GetTablesForReportAsync(wsGuid, rptGuid);
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                _logger.LogError(exc,
                    "Power BI API error in GetTablesByReportDefaultWorkspace. Status={Status}",
                    exc.Response?.StatusCode);

                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format(
                    "Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode,
                    (int)exc.Response.StatusCode,
                    exc.Response.Content,
                    exc.Response.Headers["RequestId"]?.FirstOrDefault());

                return BadRequest(new { message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Report has no associated dataset (may be paginated).");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetTablesByReportDefaultWorkspace.");
                HttpContext.Response.StatusCode = 500;
                return StatusCode(500, new { message = ex.Message, detail = ex.StackTrace });
            }
        }
        [HttpGet("lakehouses/default-workspace")]
        [MapToApiVersion("1.0")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetLakehousesDefaultWorkspaceAsync()
        {
            try
            {
                // 1. Validate Workspace Configuration
                if (!Guid.TryParse(_azureAd.Value.WorkspaceId, out var wsGuid))
                {
                    _logger.LogError("Default WorkspaceId is not configured correctly in AzureAdSettings.");
                    return StatusCode(500, new
                    {
                        message = "Default WorkspaceId is missing or invalid in configuration."
                    });
                }

                _logger.LogInformation("Fetching Lakehouse names for Workspace: {WorkspaceId}", wsGuid);

                // 2. Call the Service Function (The one we created in the previous step)
                // Ensure you have added GetLakehouseNamesAsync to your DatasetTableService
                var result = await _datasetTableService.GetLakehouseNamesAsync(wsGuid);

                return Ok(result);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Fabric API call failed for workspace.");

                // Return the specific error from the Fabric API if possible
                return StatusCode(StatusCodes.Status502BadGateway, new
                {
                    message = "Error communicating with Microsoft Fabric API.",
                    details = httpEx.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetLakehousesDefaultWorkspaceAsync.");
                return StatusCode(500, new
                {
                    message = "An internal server error occurred.",
                    error = ex.Message
                });
            }
        }
    }
}