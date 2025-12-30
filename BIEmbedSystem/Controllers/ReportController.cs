using Asp.Versioning;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using System.IO;
using System.Text.Json;
using BIEmbedSystem.Services.DTO;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    //[Authorize(policy: "RequireAdminRole")]
    // [Authorize] // Requires valid Azure AD token
    [Route("api/v{version:apiVersion}/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ILogger<ReportController> _logger;
        private readonly IOptions<AzureAdSettings> _azureAd;

        private readonly ReportPbiEmbedService _reportPbiEmbedService;
        
        public ReportController(ILogger<ReportController> logger,
            IOptions<AzureAdSettings> azureAd, ReportPbiEmbedService reportPbiEmbedService)
        {
            _logger = logger;
            _reportPbiEmbedService = reportPbiEmbedService;
            _azureAd = azureAd;
        }

        //[HttpGet("GetHeaderInfo")]
        //[MapToApiVersion("1.0")]
        //public async Task<ActionResult> GetHeaderInfo()
        //{
        //    var result = await _reportPbiEmbedService.GetHeaderInfo();
        //    _logger.LogInformation("Get API of GetHeaderInfo Version 1");
        //    return Ok(result);
        //}
        [HttpGet("embedinfo/{ReportId}/{workspaceId}/{userEmail}")]
        [MapToApiVersion("1.0")]
        public async Task<string> GetEmbedInfoAsync(string ReportId, string workspaceId, string userEmail)
        {
            try
            {
                
                _logger.LogError("AzureAd Loaded → Tenant={tenant}, Client={client}, ScopeBaseLength={len}",
                _azureAd.Value.TenantId,
                _azureAd.Value.ClientId,
                _azureAd.Value.ScopeBase?.Length);

                var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                EmbedParams embedParams = await _reportPbiEmbedService.GetEmbedParams(new Guid(workspaceId), new Guid(ReportId), token, userEmail);
                //EmbedParams embedParams = await _reportPbiEmbedService.GetEmbedParamsV2(new Guid(workspaceId), new Guid(ReportId), userEmail, token);

                return JsonSerializer.Serialize<EmbedParams>(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return message;
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return ex.Message + "\n\n" + ex.StackTrace;
            }
        }

        [HttpPost("embedinfo/{ReportId}/{workspaceId}/{userEmail}")]
        [MapToApiVersion("1.0")]
        public async Task<string> GetEmbedInfoPostAsync(string ReportId, string workspaceId, string userEmail, [FromBody] EmbedRequest request)
        {
            try
            {

                _logger.LogError("AzureAd Loaded → Tenant={tenant}, Client={client}, ScopeBaseLength={len}",
                _azureAd.Value.TenantId,
                _azureAd.Value.ClientId,
                _azureAd.Value.ScopeBase?.Length);

                //var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
                //EmbedParams embedParams = await _reportPbiEmbedService.GetEmbedParams(new Guid(workspaceId), new Guid(ReportId), token,userEmail);
                EmbedParams embedParams = await _reportPbiEmbedService.GetEmbedParamsV2(new Guid(workspaceId), new Guid(ReportId), userEmail, request.Token);

                return JsonSerializer.Serialize<EmbedParams>(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return message;
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return ex.Message + "\n\n" + ex.StackTrace;
            }
        }

        [HttpPost("embedinfo/exportfile")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> ExportReportFileAsync([FromQuery] string ReportId, string workspaceId, [FromBody] ExportReportRequest request)
        {
            try
            {
                // web front end
                Guid workspaceGuid = new Guid(workspaceId);
                Guid reportGuid = new Guid(ReportId);

                var response = await _reportPbiEmbedService.ExportReportAsync(workspaceGuid, reportGuid, request);

                // Resolve FileFormat robustly (supports enum name or numeric enum value)
                Microsoft.PowerBI.Api.Models.FileFormat? resolvedFormat = null;
                var raw = request.Format.ToString();

                if (!string.IsNullOrEmpty(raw))
                {
                    // Try parse by name first (e.g. "PDF", "PNG")
                    if (Enum.TryParse<Microsoft.PowerBI.Api.Models.FileFormat>(raw, true, out var byName))
                    {
                        resolvedFormat = byName;
                    }
                    else if (int.TryParse(raw, out var numeric) && Enum.IsDefined(typeof(Microsoft.PowerBI.Api.Models.FileFormat), numeric))
                    {
                        resolvedFormat = (Microsoft.PowerBI.Api.Models.FileFormat)numeric;
                    }
                }

                // Map enum to content-type and extension
                string contentType;
                string ext;
                switch (resolvedFormat)
                {
                    case Microsoft.PowerBI.Api.Models.FileFormat.PDF:
                    case Microsoft.PowerBI.Api.Models.FileFormat.ACCESSIBLEPDF:
                        contentType = "application/pdf";
                        ext = "pdf";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.PNG:
                    case Microsoft.PowerBI.Api.Models.FileFormat.IMAGE:
                        contentType = "image/png";
                        ext = "png";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.PPTX:
                        contentType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                        ext = "pptx";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.XLSX:
                        contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        ext = "xlsx";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.DOCX:
                        contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        ext = "docx";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.CSV:
                        contentType = "text/csv";
                        ext = "csv";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.XML:
                        contentType = "application/xml";
                        ext = "xml";
                        break;

                    case Microsoft.PowerBI.Api.Models.FileFormat.MHTML:
                        contentType = "multipart/related";
                        ext = "mhtml";
                        break;

                    default:
                        contentType = "application/octet-stream";
                        ext = "bin";
                        break;
                }

                var fileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.{ext}";
                return File(response, contentType, fileName);
            }
            catch (HttpOperationException exc)
            {
                if (exc.Response.Content.Contains("OperationIsNotSupportedForPremiumFilesModel"))
                {
                    return BadRequest("Export is not supported for Premium Files Model (Direct Lake or Large Model) reports in Power BI.");
                }

                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content,
                    exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
        [HttpGet("getWorkspaceInfo")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetWorkspaceInfo()
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetWorkspaceInfo();
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
        [HttpGet("getReportList")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetReportListAsync()
        {
            try
            {
                _logger.LogError("AzureAd Loaded → Tenant={tenant}, Client={client}, ScopeBaseLength={len}",
                _azureAd.Value.TenantId,
                _azureAd.Value.ClientId,
                _azureAd.Value.ScopeBase?.Length);
                var embedParams = await _reportPbiEmbedService.GetReportList(new Guid(_azureAd.Value.WorkspaceId));
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
        [HttpGet("getReportListByWorkspace")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetReportListByWorkspaceAsync(string workspaceId)
        {
            try
            {
                var result = await _reportPbiEmbedService.GetReportList(new Guid(workspaceId != null ? workspaceId : _azureAd.Value.WorkspaceId));
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
      
        [HttpGet("getReportsPagesList")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetReportsPagesListAsync(string workspaceId,string reportId)
        {
            try
            {
                var result = await _reportPbiEmbedService.GetReportsPagesList(new Guid(workspaceId != null ? workspaceId : _azureAd.Value.WorkspaceId), new Guid(reportId));
                return Ok(result);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        
        [HttpGet("getDatasetHistory")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetDatasetHistoryAsync(string datasetId)
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetDatasetHistory(new Guid(_azureAd.Value.WorkspaceId), datasetId);
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
        [HttpGet("getDatasetRefresh")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetDatasetRefreshAsync(string datasetId)
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetDatasetRefresh(new Guid(_azureAd.Value.WorkspaceId), datasetId);
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        [HttpGet("getReportSubscription")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetReportSubscription(string ReportId)
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetReportSubscription(new Guid(ReportId));
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        [HttpGet("getSubscriptions")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetSubscriptionsAsync()
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetSubscriptions();
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        [HttpPost("report-share")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> ShareReportV2([FromBody] ShareReportRequest req)
        {
            await _reportPbiEmbedService.SendReportShareEmail(req);
            return Ok(new { message = "Email sent successfully" });
        }

        [HttpGet("bookmark/{userId:int}/{reportId}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> GetBookmarks(int userId, string reportId)
        {
            var bookmarks = await _reportPbiEmbedService.GetUserBookmarksAsync(userId, reportId);
            return Ok(bookmarks);
        }

        [HttpDelete("bookmark/{id:long}/{userId:int}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> DeleteBookmark(long id, int userId)
        {
            var deleted = await _reportPbiEmbedService.DeleteBookmarkAsync(id, userId);

            if (!deleted)
                return NotFound(new { message = "Bookmark not found" });

            return Ok(new { message = "Bookmark deleted successfully" });
        }

        // ---------------- CREATE ----------------
        [HttpPost("bookmark/create")]
        [MapToApiVersion("1.0")]

        public async Task<IActionResult> CreateBookmark([FromBody] BookmarkRequestDto dto)
        {
            var result = await _reportPbiEmbedService.CreateBookmarkAsync(dto);
            return Ok(result);
        }

        // ---------------- UPDATE ----------------
        [HttpPut("bookmark/update")]
        public async Task<IActionResult> UpdateBookmark([FromBody] UpdateBookmarkDto dto)
        {
            var result = await _reportPbiEmbedService.UpdateBookmarkAsync(dto);

            if (result == null)
                return NotFound(new { message = "Bookmark not found" });

            return Ok(result);
        }
    }
}
