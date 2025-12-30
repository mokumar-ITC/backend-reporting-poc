using Asp.Versioning;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/roles")]
    public class RoleController : ControllerBase
    {
        private readonly RoleService _service;

        public RoleController(RoleService service)
        {
            _service = service;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), 200)]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        [HttpGet("org/{orgId:int}")]
        [ProducesResponseType(typeof(IEnumerable<RoleDto>), 200)]
        public async Task<IActionResult> GetByOrg(int orgId)
            => Ok(await _service.GetByOrgAsync(orgId));

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(RoleDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var role = await _service.GetByIdAsync(id);
            if (role == null) return NotFound();

            return Ok(role);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RoleDto), 201)]
        public async Task<IActionResult> Create(RoleCreateRequest request)
        {
            var role = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = role.Id }, role);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(RoleDto), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(int id, RoleUpdateRequest request)
        {
            var role = await _service.UpdateAsync(id, request);
            if (role == null) return NotFound();

            return Ok(role);
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id, [FromQuery] int? updatedBy)
        {
            var deleted = await _service.DeleteAsync(id, updatedBy);
            if (!deleted) return NotFound();

            return NoContent();
        }

        [HttpPut("bulk")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> BulkUpdate(RoleBulkUpdateRequest request)
        {
            var updatedCount = await _service.BulkUpdateAsync(request);
            return Ok(new { Updated = updatedCount });
        }

        [HttpPost("bulk")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BulkUpload(
            [FromForm] RoleBulkUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("CSV file is required.");

            var result = await _service.BulkUploadAsync(request.File);
            return Created("", result);
        }
    }
}
