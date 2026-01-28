using Asp.Versioning;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class OrganizationSubscriptionController : ControllerBase
    {
        private readonly IOrganizationSubscriptionService _service;
        public OrganizationSubscriptionController(IOrganizationSubscriptionService service) { _service = service; }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrganizationSubscriptionRequest request)
        {
            var resp = await _service.CreateAsync(request);
            return Ok(resp);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resp = await _service.GetAllSubcription();
            return Ok(resp);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateOrganizationSubscriptionRequest request)
        {
            var resp = await _service.UpdateAsync(id, request);
            return Ok(resp);
        }

        [HttpGet("org/{organizationId}")]
        public async Task<IActionResult> GetByOrg(int organizationId)
        {
            var resp = await _service.GetByOrganizationAsync(organizationId);
            return Ok(resp);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resp = await _service.GetByIdAsync(id);
            return Ok(resp);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }
    }

}
