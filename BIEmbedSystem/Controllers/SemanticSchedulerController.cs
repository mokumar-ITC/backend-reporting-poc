using Asp.Versioning;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/semantic-schedulers")]
    public class SemanticSchedulerController : ControllerBase
    {
        private readonly ISemanticSchedulerService _service;

        public SemanticSchedulerController(ISemanticSchedulerService service)
        {
            _service = service;
        }

        [HttpPost("{userId:int}")]
        public async Task<IActionResult> Create(int userId,
            [FromBody] SemanticSchedulerCreateDto dto)
        {
            var user = User.Identity?.Name ?? "System";
            var id = await _service.CreateAsync(dto, userId);
            return Ok(new { Id = id });
        }

        [HttpPut("{id}/{userId:int}")]
        public async Task<IActionResult> Update(
            int id,
            int userId,
            [FromBody] SemanticSchedulerUpdateDto dto)
        {
            var user = User.Identity?.Name ?? "System";
            var result = await _service.UpdateAsync(id, dto, userId);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpPatch("{id}/status/{userId:int}")]
        public async Task<IActionResult> UpdateStatus(
            int id,
            int userId,
            [FromBody] SemanticSchedulerStatusDto dto)
        {
            var user = User.Identity?.Name ?? "System";
            var result = await _service.UpdateStatusAsync(id, dto.IsActive, userId);
            if (!result) return NotFound();
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _service.DeleteAsync(id);
            if (!result) return NotFound();
            return Ok();
        }

        // ----------------------------------------------------
        // GET: /v1/semantic-schedulers/{workspaceId}/{datasetId}
        // ----------------------------------------------------
        [HttpGet("{workspaceId:guid}/{datasetId:guid}")]
        public async Task<IActionResult> GetByWorkspaceAndDataset(
            Guid workspaceId,
            Guid datasetId
        )
        {
            var data = await _service.GetByWorkspaceAndDatasetAsync(
                workspaceId,
                datasetId
            );

            return Ok(data);
        }

        // ----------------------------------------------------
        // GET: /v1/semantic-schedulers/user/{userId}
        // ----------------------------------------------------
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUser(int userId)
        {
            var data = await _service.GetByUserAsync(userId);
            return Ok(data);
        }

        // ----------------------------------------------------
        // ✅ GET by Id
        // ----------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var scheduler = await _service.GetByIdAsync(id);

            if (scheduler == null)
                return NotFound();

            return Ok(scheduler);
        }
    }

}
