using Asp.Versioning;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class UserSubscriptionController : ControllerBase
    {
        private readonly IUserSubscriptionService _service;
        public UserSubscriptionController(IUserSubscriptionService service) { _service = service; }

        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] CreateUserSubscriptionRequest request)
        {
            var ok = await _service.AssignUserAsync(request);
            if (!ok) return BadRequest("User already assigned");
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Unassign(int id)
        {
            var ok = await _service.UnassignUserAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("orgsub/{orgSubscriptionId}")]
        public async Task<IActionResult> GetUsers(int orgSubscriptionId)
        {
            var users = await _service.GetUsersInOrgSubscriptionAsync(orgSubscriptionId);
            return Ok(users);
        }
    }

}
