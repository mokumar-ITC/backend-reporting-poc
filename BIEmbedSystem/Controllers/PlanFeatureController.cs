using Asp.Versioning;
using Azure.ResourceManager.Fabric;
using Azure.ResourceManager.Fabric.Models;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;



namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PlanFeatureController : ControllerBase
    {
        private readonly IPlanFeatureService _service;

        public PlanFeatureController(IPlanFeatureService service)
        {
            _service = service;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePlanFeatureRequest request)
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }

        // GET ALL FEATURES FOR PLAN
        [HttpGet("{planId}")]
        public async Task<IActionResult> GetByPlan(int planId)
        {
            var result = await _service.GetByPlanAsync(planId);
            return Ok(result);
        }

        // UPDATE
        [HttpPut("{planFeatureId}")]
        public async Task<IActionResult> Update(int planFeatureId, [FromBody] UpdatePlanFeatureRequest request)
        {
            var result = await _service.UpdateAsync(planFeatureId, request);
            return Ok(result);
        }

        // DELETE
        [HttpDelete("{planFeatureId}")]
        public async Task<IActionResult> Delete(int planFeatureId)
        {
            var success = await _service.DeleteAsync(planFeatureId);

            if (!success) return NotFound();

            return Ok(new { message = "Feature removed from plan." });
        }
        // GET: /api/v1.0/subscription-plans/{planId}
        [HttpGet("{planId:int}/detail")]
        [ProducesResponseType(typeof(PlanFeaturesResponseDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetPlanFeatures(int planId)
        {
            var data = await _service.GetPlanFeaturesAsync(planId);

            if (data == null)
                return NotFound($"Plan with ID {planId} not found.");

            return Ok(data);
        }

        // GET: /api/v1.0/subscription-plans/{planId}/details
        [HttpGet("{planId:int}/details")]
        [ProducesResponseType(typeof(SubscriptionPlanFullDetailsDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetFullPlanDetails(int planId)
        {
            var result = await _service.GetFullPlanDetailsAsync(planId);

            if (result == null)
                return NotFound($"Subscription plan with ID {planId} not found.");

            return Ok(result);
        }

    }

}
