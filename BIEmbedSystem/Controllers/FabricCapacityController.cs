using Asp.Versioning;
using Azure.ResourceManager.Fabric;
using Azure.ResourceManager.Fabric.Models;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Asn1.Crmf;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/capacities")]
    public class FabricCapacityController : ControllerBase
    {
        private readonly ILogger<FabricCapacityController> _logger;
        private readonly FabricCapacityService _capacityService;

        public FabricCapacityController(ILogger<FabricCapacityController> logger, FabricCapacityService capacityService)
        {
            _logger = logger;
            _capacityService = capacityService;
        }

        // --- LIST & GET Operations (Read) ---

        // 1. List By Subscription
        // GET /api/v1.0/capacities/subscriptions/{subscriptionId}
        [HttpGet("subscriptions/{subscriptionId}")]
        [ProducesResponseType(typeof(IEnumerable<FabricCapacityData>), 200)]
        public async Task<ActionResult> ListBySubscription(string subscriptionId)
        {
            var capacities = await _capacityService.ListCapacitiesBySubscriptionAsync(subscriptionId);
            return Ok(capacities);
        }

        // 2. List By Resource Group
        // GET /api/v1.0/capacities/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}
        [HttpGet("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}")]
        [ProducesResponseType(typeof(IEnumerable<FabricCapacityData>), 200)]
        public async Task<ActionResult> ListByResourceGroup(string subscriptionId, string resourceGroupName)
        {
            var capacities = await _capacityService.ListCapacitiesByResourceGroupAsync(subscriptionId, resourceGroupName);
            return Ok(capacities);
        }

        // 3. Get a FabricCapacity
        // GET /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}
        [HttpGet("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}")]
        [ProducesResponseType(typeof(FabricCapacityData), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetCapacity(
            string subscriptionId,
            string resourceGroupName,
            string capacityName)
        {
            var capacity = await _capacityService.GetCapacityAsync(subscriptionId, resourceGroupName, capacityName);

            if (capacity == null) return NotFound();

            return Ok(capacity);
        }

        // --- CREATE, UPDATE, DELETE Operations (Write) ---

        // 4. Create Or Update (Create/Replace)
        // PUT /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}
        [HttpPut("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}")]
        [ProducesResponseType(typeof(FabricCapacityData), 200)]
        public async Task<ActionResult> CreateOrUpdateCapacity(
            string subscriptionId,
            string resourceGroupName,
            string capacityName,
            [FromBody] FabricCapacityCreationData data)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var capacity = await _capacityService.CreateOrUpdateCapacityAsync(
                subscriptionId,
                resourceGroupName,
                capacityName,
                data);

            return Ok(capacity);
        }

        // 5. Update (Patch)
        // PATCH /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}
        [HttpPatch("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}")]
        [ProducesResponseType(typeof(FabricCapacityData), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> UpdateCapacity(
            string subscriptionId,
            string resourceGroupName,
            string capacityName,
            [FromBody] FabricCapacityPatchData data)
        {
            var capacity = await _capacityService.UpdateCapacityAsync(
                subscriptionId,
                resourceGroupName,
                capacityName,
                data);

            if (capacity == null) return NotFound();

            return Ok(capacity);
        }

        // 6. Delete
        // DELETE /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}
        [HttpDelete("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}")]
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteCapacity(
            string subscriptionId,
            string resourceGroupName,
            string capacityName)
        {
            await _capacityService.DeleteCapacityAsync(subscriptionId, resourceGroupName, capacityName);
            return Accepted();
        }

        // --- LIFECYCLE Operations ---

        // 7. Resume
        // POST /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}/resume
        [HttpPost("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}/resume")]
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ResumeCapacity(string subscriptionId, string resourceGroupName, string capacityName)
        {
            await _capacityService.ResumeCapacityAsync(subscriptionId, resourceGroupName, capacityName);
            return Accepted();
        }

        // 8. Suspend
        // POST /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}/suspend
        [HttpPost("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}/suspend")]
        [ProducesResponseType(202)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> SuspendCapacity(string subscriptionId, string resourceGroupName, string capacityName)
        {
            await _capacityService.SuspendCapacityAsync(subscriptionId, resourceGroupName, capacityName);
            return Accepted();
        }

        // --- SKU Operations ---

        // 9. List SKUs
        // GET /api/v1.0/capacities/skus
        [HttpGet("skus")]
        [ProducesResponseType(typeof(IEnumerable<FabricSkuDetailsForNewCapacity>), 200)]
        public async Task<ActionResult> ListSkus()
        {
            var skus = await _capacityService.ListSkusAsync();
            return Ok(skus);
        }

        // 10. List SKUs For Capacity
        // POST /api/v1.0/capacities/subscriptions/{subId}/resourceGroups/{rgName}/capacities/{capacityName}/listSkus
        [HttpPost("subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/capacities/{capacityName}/listSkus")]
        [ProducesResponseType(typeof(IEnumerable<FabricSkuDetailsForExistingCapacity>), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ListSkusForCapacity(string subscriptionId, string resourceGroupName, string capacityName)
        {
            var skus = await _capacityService.ListSkusForCapacityAsync(subscriptionId, resourceGroupName, capacityName);

            // Note: If the capacity is not found, the service returns an empty list, 
            // but for a clear API contract, you might check for existence first 
            // or let the service handle the 404 (as currently done).
            if (!skus.Any() && await _capacityService.GetCapacityAsync(subscriptionId, resourceGroupName, capacityName) == null)
            {
                return NotFound($"Capacity '{capacityName}' not found.");
            }

            return Ok(skus);
        }

        // 11. Check Name Availability (updated with correct type)
        // POST /api/v1.0/capacities/subscriptions/{subscriptionId}/checkNameAvailability?location={location}
        [HttpPost("subscriptions/{subscriptionId}/location/{location}/checkNameAvailability")]
        [ProducesResponseType(typeof(FabricNameAvailabilityResult), 200)] // <--- CORRECTED TYPE
        public async Task<ActionResult> CheckNameAvailability(
            string subscriptionId,
            string location,
            [FromBody] FabricCapacityNameAvailabilityRequest request)
        {
            if (string.IsNullOrEmpty(request.Name)) return BadRequest("Capacity Name must be provided.");

            var result = await _capacityService.CheckNameAvailabilityAsync(
                subscriptionId,
                location,
                request.Name);

            return Ok(result);
        }
    

   
        // 1️⃣ CREATE Scheduler Entry
        // POST /api/v1.0/capacities/scheduler
        [HttpPost("scheduler")]
        [ProducesResponseType(typeof(CapacitySchedulerModel), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> CreateScheduler([FromBody] CapacitySchedulerCreateRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _capacityService.CreateSchedulerAsync(request);

            return CreatedAtAction(nameof(GetSchedulerById), new { id = result.Id }, result);
        }

        // POST /api/v1.0/capacities/scheduler
        [HttpGet("scheduler/{capacityName}")]
        [ProducesResponseType(typeof(List<CapacitySchedulerModel>), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<List<CapacitySchedulerModel>>> GetAllScheduler(string capacityName)
        {
            if (string.IsNullOrWhiteSpace(capacityName))
                return BadRequest("Capacity name cannot be empty.");

            var result = await _capacityService.GetAllSchedulerAsync(capacityName);

            if (result == null || result.Count == 0)
                return NotFound($"No schedulers found for capacity '{capacityName}'.");

            return Ok(result);
        }


        // 2️⃣ GET Scheduler by ID
        // GET /api/v1.0/capacities/scheduler/{id}
        [HttpGet("scheduler/{id:int}")]
        [ProducesResponseType(typeof(CapacitySchedulerModel), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetSchedulerById(int id)
        {
            var scheduler = await _capacityService.GetSchedulerByIdAsync(id);

            if (scheduler == null) return NotFound($"Scheduler with ID {id} not found.");
            return Ok(scheduler);
        }

        // 3️⃣ UPDATE Scheduler (Activate/Inactivate or update times)
        // PUT /api/v1.0/capacities/scheduler/{id}
        [HttpPut("scheduler/{id:int}")]
        [ProducesResponseType(typeof(CapacitySchedulerModel), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> UpdateScheduler(
            int id,
            [FromBody] CapacitySchedulerUpdateRequest request)
        {
            var updated = await _capacityService.UpdateSchedulerAsync(id, request);

            if (updated == null) return NotFound($"Scheduler with ID {id} not found.");

            return Ok(updated);
        }

        // 4️⃣ DELETE Scheduler
        // DELETE /api/v1.0/capacities/scheduler/{id}
        [HttpDelete("scheduler/{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> DeleteScheduler(int id)
        {
            var deleted = await _capacityService.DeleteSchedulerAsync(id);

            if (!deleted) return NotFound($"Scheduler with ID {id} not found.");

            return NoContent();

        }

        

    }

    // Simple model for the POST/QUERY request body (from previous answer)
    public class FabricCapacityNameAvailabilityRequest
    {
        public string Name { get; set; } = string.Empty;
    }



}