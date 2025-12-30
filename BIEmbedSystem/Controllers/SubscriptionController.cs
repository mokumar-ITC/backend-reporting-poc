using Asp.Versioning;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Core.Entities;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/subscriptions")]
    public class SubscriptionController : ControllerBase
    {
        private readonly ILogger<SubscriptionController> _logger;
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionController(
            ILogger<SubscriptionController> logger,
            SubscriptionService subscriptionService)
        {
            _logger = logger;
            _subscriptionService = subscriptionService;
        }

        // -------------------------------------------------------
        // 1️⃣ CREATE Subscription
        // POST: /api/v1.0/subscriptions
        // -------------------------------------------------------
        [HttpPost]
        [ProducesResponseType(typeof(ReportSubscriptionDto), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> CreateSubscription([FromBody] ReportSubscriptionDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _subscriptionService.CreateSubscriptionAsync(dto);

            // ✔ Return 201 Created with JSON body
            return StatusCode(201, result);
        }


        // -------------------------------------------------------
        // 2️⃣ GET All Subscriptions
        // GET: /api/v1.0/subscriptions
        // -------------------------------------------------------
        [HttpGet]
        [ProducesResponseType(typeof(List<ReportSubscriptionDto>), 200)]
        public async Task<ActionResult<List<ReportSubscriptionDto>>> GetAllSubscriptions()
        {
            var list = await _subscriptionService.GetAllSubscriptionsAsync();
            return Ok(list);
        }

        [HttpGet("{workspaceId:guid}/{reportId:guid}")]
        [ProducesResponseType(typeof(List<ReportSubscriptionDto>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<List<ReportSubscriptionDto>>>GetSubscriptionsByWorkspaceAndReport(Guid workspaceId, Guid reportId)
        {
            var list = await _subscriptionService
                .GetSubscriptionsByWorkspaceAndReportAsync(workspaceId, reportId);

            if (list == null || !list.Any())
                return NotFound("No subscriptions found for the given workspace and report.");

            return Ok(list);
        }


        // -------------------------------------------------------
        // 3️⃣ GET Subscription by ID
        // GET: /api/v1.0/subscriptions/{id}
        // -------------------------------------------------------
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ReportSubscriptionDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetSubscriptionById(int id)
        {
            var result = await _subscriptionService.GetSubscriptionByIdAsync(id);

            if (result == null)
                return NotFound($"Subscription with ID {id} not found.");

            return Ok(result);
        }

        // -------------------------------------------------------
        // 3️⃣ GET Subscription by ID
        // GET: /api/v1.0/subscriptions/{id}
        // -------------------------------------------------------
        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(typeof(ReportSubscriptionDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetSubscriptionByUserId(int userId)
        {
            var result = await _subscriptionService.GetSubscriptionByUserIdAsync(userId);

            if (result == null)
                return NotFound($"Subscription with ID {userId} not found.");

            return Ok(result);
        }

        // -------------------------------------------------------
        // 4️⃣ UPDATE Subscription
        // PUT: /api/v1.0/subscriptions/{id}
        // -------------------------------------------------------
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> UpdateSubscription(int id, [FromBody] ReportSubscriptionDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID in URL does not match ID in body.");

            var updated = await _subscriptionService.UpdateSubscriptionAsync(dto);

            if (!updated)
                return NotFound($"Subscription with ID {id} not found.");

            return Ok(dto);
        }

        // -------------------------------------------------------
        // 5️⃣ DELETE Subscription
        // DELETE: /api/v1.0/subscriptions/{id}
        // -------------------------------------------------------
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteSubscription(int id)
        {
            var deleted = await _subscriptionService.DeleteSubscriptionAsync(id);

            if (!deleted)
                return NotFound($"Subscription with ID {id} not found.");

            return NoContent();
        }

        // -------------------------------------------------------
        // 6️⃣ ACTIVATE / DEACTIVATE
        // PATCH: /api/v1.0/subscriptions/{id}/status?active=true/false
        // -------------------------------------------------------
        [HttpPatch("{id:int}/status")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> SetActiveStatus(int id, [FromQuery] bool active)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);

            if (subscription == null)
                return NotFound($"Subscription with ID {id} does not exist.");

            subscription.IsActive = active;
            await _subscriptionService.UpdateSubscriptionAsync(subscription);

            return Ok(new { id, isActive = active });
        }

        // -------------------------------------------------------
        // 7️⃣ TRIGGER Subscription & Email PDF (Manual Trigger)
        // POST: /api/v1.0/subscriptions/{id}/trigger
        // -------------------------------------------------------
        [HttpPost("{id:int}/trigger")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> TriggerSubscription(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);

            if (subscription == null)
                return NotFound($"Subscription with ID {id} not found.");

            // Call Power BI export service here
            // (Assumes you already wrote export → email logic)
            // await _subscriptionService.TriggerNowAsync(id);

            return Ok(new
            {
                message = "Subscription triggered successfully.",
                subscriptionId = id
            });
        }
    }
}
