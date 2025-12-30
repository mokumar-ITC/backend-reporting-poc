using Asp.Versioning;
using BIEmbedSystem.Services.DTO;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/subscription-plans")]
    public class SubscriptionPlanController : ControllerBase
    {
        private readonly SubscriptionPlanService _service;

        public SubscriptionPlanController(SubscriptionPlanService service)
        {
            _service = service;
        }

        // GET ALL
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanDto>), 200)]
        public async Task<ActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        // GET BY ID
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SubscriptionPlanDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetById(int id)
        {
            var plan = await _service.GetByIdAsync(id);
            if (plan == null) return NotFound($"Plan ID {id} not found.");

            return Ok(plan);
        }

        // CREATE
        [HttpPost]
        [ProducesResponseType(typeof(SubscriptionPlanDto), 201)]
        public async Task<ActionResult> Create([FromBody] SubscriptionPlanCreateRequest request)
        {
            var created = await _service.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = created.PlanId }, created);
        }

        // UPDATE
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(SubscriptionPlanDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Update(int id, [FromBody] SubscriptionPlanUpdateRequest request)
        {
            var updated = await _service.UpdateAsync(id, request);
            if (updated == null) return NotFound($"Plan ID {id} not found.");

            return Ok(updated);
        }

        // DELETE
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound($"Plan ID {id} not found.");

            return NoContent();
        }

        // GET: /api/v1.0/subscription-plans/active/details
        [HttpGet("active/details")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanFullDetailsDto>), 200)]
        public async Task<ActionResult> GetAllActivePlanDetails()
        {
            var result = await _service.GetAllActivePlansWithDetailsAsync();
            return Ok(result);
        }

        [HttpGet("all/details")]
        [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanFullDetailsDto>), 200)]
        public async Task<ActionResult> GetAllPlanDetails()
        {
            var result = await _service.GetAllPlansWithDetailsAsync();
            return Ok(result);
        }

    }

}
