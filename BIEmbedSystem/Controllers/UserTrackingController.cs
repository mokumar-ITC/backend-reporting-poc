using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;

namespace BIEmbedSystem.API.Controllers
{
    

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/tracking")]
    public class UserTrackingController : ControllerBase
    {
        private readonly UserTrackingService _service;

        public UserTrackingController(UserTrackingService service)
        {
            _service = service;
        }

        // Log user action
        [HttpPost]
        public async Task<ActionResult> Log([FromBody] UserTrackingRequest request)
        {
            await _service.LogActionAsync(request);
            return Ok("Action logged successfully");
        }

        // Get user history
        [HttpGet("{userId:int}")]
        public async Task<ActionResult> GetHistory(int userId)
        {
            var result = await _service.GetHistoryAsync(userId);
            return Ok(result);
        }

        [HttpGet("refresh/{workspaceId}/{reportId}/{userId:int}")]
        public async Task<ActionResult> GetRefreshLog(string workspaceId, string reportId, int userId)
        {
            var result = await _service.GetRefreshLogAysnc(workspaceId, reportId, userId);
            return Ok(result);
        }


        // Get user history
        [HttpGet("list/{organizationId}")]
        public async Task<ActionResult> GetAllHistory(int organizationId)
        {
            var result = await _service.GetAllHistoryAsync(organizationId);
            return Ok(result);
        }
    }

}
