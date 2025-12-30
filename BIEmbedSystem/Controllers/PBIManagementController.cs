using Asp.Versioning;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.Rest;
using System.Data;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PBIManagementController : ControllerBase
    {
        private readonly ILogger<PBIManagementController> _logger;
        private readonly PBIManagementService _pbiMmgtService;
        private readonly ReportPbiEmbedService _reportPbiEmbedService;
        private readonly AadService _aadService;

        public PBIManagementController(ILogger<PBIManagementController> logger, PBIManagementService pbiMmgtService, ReportPbiEmbedService reportPbiEmbedService, AadService aadService)
        {
            _logger = logger;
            _pbiMmgtService = pbiMmgtService;
            _reportPbiEmbedService = reportPbiEmbedService;
            _aadService = aadService;
        }

        [HttpPost("save-groupWorkspaceReport")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> SaveGroupWorkspaceReports([FromBody] PBIGroupWorkspaceReport groupWorkspaceReports)
        {
            if (groupWorkspaceReports == null)
                return BadRequest("GroupWorkspaceReports is empty.");

            var res = await _pbiMmgtService.SaveGroupWorkspaceReport(groupWorkspaceReports);           
            return Ok(res);
        }
        [HttpGet("get-groupWorkspaceReport")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetGroupWorkspaceReport()
        {
            var res = await _pbiMmgtService.GetGroupWorkspaceReport();
            _logger.LogInformation("Get GroupWorkspaceReport of Version 1" + res);            
            return Ok(res);
        }
        [HttpPost("save-workspaceReport")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> SaveWorkspaceReport([FromBody] PBIWorkspaceReport workspaceReport)
        {
            if (workspaceReport == null)
                return BadRequest("WorkspaceReports is empty.");

            var res = await _pbiMmgtService.SaveWorkspaceReport(workspaceReport);
            return Ok(res);
        }
        [HttpGet("get-workspaceReport")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetWorkspaceReport()
        {
            var res = await _pbiMmgtService.GetWorkspaceReport();
            _logger.LogInformation("Get WorkspaceReport of  Version 1" + res);
            return Ok(res);
        }
        [HttpPost("save-menubarByGroup")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> SaveMenubarByGroup([FromBody] PBIMenubarByGroup menubarByGroup)
        {
            if (menubarByGroup == null)
                return BadRequest("MenubarByGroup is empty.");

            var res = await _pbiMmgtService.SaveMenubarByGroup(menubarByGroup);
            return Ok(res);
        }
        [HttpGet("get-menubarByGroup")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetMenubarByGroup()
        {
            var res = await _pbiMmgtService.GetMenubarByGroup();
            _logger.LogInformation("GetMenubarByGroup of Version 1" + res);
            return Ok(res);
        }
        [HttpPost("save-navigationManagement")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> SavePBINavigationManagement([FromBody] PBINavigationManagement navigationManagement)
        {
            var userEmail = User.GetDisplayName();
            if (navigationManagement == null)
                return BadRequest("navigationManagement is empty.");

            var res = await _pbiMmgtService.SavePBINavigationManagement(navigationManagement,userEmail);
            return Ok(res);
        }
        [HttpGet("get-navigationManagement")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetPBINavigationManagement()
        {
            var res = await _pbiMmgtService.GetPBINavigationManagement();
            _logger.LogInformation("get navigationManagement of Version 1" + res);
            return Ok(res);
        }
        [HttpGet("get-navigationManagementById")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetPBINavigationManagementById(int Id)
        {
            var res = await _pbiMmgtService.GetPBINavigationManagementById(Id);
            _logger.LogInformation("get navigationManagement of Version 1" + res);
            return Ok(res);
        }

        [HttpDelete("delete-navigationManagementById")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> DeletePBINavigationManagementById(int Id)
        {
            var res = await _pbiMmgtService.DeletePBINavigationManagementById(Id);
            _logger.LogInformation("get navigationManagement of Version 1" + res);
            return Ok(res);
        }
        [HttpPost("get-userMenuByGroup")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetUserMenuByGroup([FromBody] List<int> groupName)
        {
            
            var res = await _pbiMmgtService.GetUserMenuByGroup(groupName);
            _logger.LogInformation("get userMenuByGroup of Version 1" + res);
            return Ok(res);
        }

        [HttpPost("save-navigationUserAccess")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> SavePBINavigationUserAccess([FromBody] PBINavigationUserAccess navigationUserAccess)
        {
            
            //string? userEmail = User?.GetDisplayName(); // Ensure User is not null
            //if (userEmail == null)
            //    return BadRequest("User email is null.");

            if (navigationUserAccess == null)
                return BadRequest("navigationUserAccess is empty.");

            var res = await _pbiMmgtService.SavePBINavigationUserAccess(navigationUserAccess, navigationUserAccess.CreatedBy);
            return Ok(res);
        }
        [HttpGet("get-navigationUserAccess")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetPBINavigationUserAccess()
        {
            var res = await _pbiMmgtService.GetPBINavigationUserAccess();
            _logger.LogInformation("get navigationUserAccess of Version 1" + res);
            return Ok(res);
        }
        
        [HttpGet("get-navigationUserAccessByEmail")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetPBINavigationUserAccessByEmail(string userEmail)
        {
            var res = await _pbiMmgtService.GetPBINavigationAccessByUser(userEmail);
            _logger.LogInformation("get navigationManagement of Version 1" + res);
            return Ok(res);
        }

        [HttpGet("get-navigationUserAccessByOrg/{orgId:int}")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetPBINavigationUserAccessByOrg(int orgId)
        {
            var res = await _pbiMmgtService.GetPBINavigationAccessByOrg(orgId);
            _logger.LogInformation("get navigationManagement of Version 1" + res);
            return Ok(res);
        }

        //create the api for Multi PBI management
        [HttpGet("get-refreshable-for-capacity")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<Refreshables>> GetRefreshablesForCapacityAsync(Guid capacityId, string refreshableId)
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.GetSingleRefreshableRawAsync(capacityId, refreshableId);
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

        [HttpGet("get-refreshable")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<Refreshables>> GetRefreshablesAsync(int top, string expand, string filter, int skip)
        {
            try
            {
                var embedParams = await _reportPbiEmbedService.ListAllRefreshablesAsync(top,expand,filter,skip);
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

        /// <summary>
        /// Returns an Azure AD access token (app-only) for Power BI.
        /// If you instead want an embed token, swap the service call to GetEmbedTokenAsync(...) and adapt parameters.
        /// </summary>
        [HttpGet("getAccessToken")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<object>> GetAccessToken()
        {
            try
            {
                // Call service to acquire the AAD token.
                // Implement IPowerBiService.GetAccessTokenAsync() to return the raw AAD JWT.
                var accessToken = await _aadService.GetAccessTokenAsync();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogWarning("GetAccessToken returned null or empty token.");
                    return StatusCode(500, "Failed to acquire access token.");
                }

                // Return token (wrapped in an object so you can extend with expiry/other metadata later)
                return Ok(new
                {
                    accessToken
                });
            }
            catch (HttpOperationException exc)
            {
                // Mirror the behavior in your sample: set response code and return a formatted message
                var statusCode = (int)exc.Response.StatusCode;
                HttpContext.Response.StatusCode = statusCode;

                var requestId = exc.Response.Headers != null && exc.Response.Headers.ContainsKey("RequestId")
                    ? exc.Response.Headers["RequestId"].FirstOrDefault()
                    : null;

                var message = string.Format(
                    "Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}",
                    exc.Response.StatusCode,
                    statusCode,
                    exc.Response.Content,
                    requestId
                );

                _logger.LogError(exc, "HttpOperationException in GetAccessToken: {Message}", message);
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                // Generic error handling
                _logger.LogError(ex, "Unhandled exception in GetAccessToken");
                HttpContext.Response.StatusCode = 500;
                return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPut("bulk")]
        public async Task<IActionResult> BulkUpdateNavigationUserAccess(
        [FromBody] BulkNavigationUserAccessUpdateRequest request)
        {
            try
            {
                var result = await _pbiMmgtService
                    .BulkUpdatePBINavigationUserAccess(request);

                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
