using Asp.Versioning;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/organizations")]
    public class OrganizationController : ControllerBase
    {
        private readonly OrganizationService _service;
        private OrganizationDto? any;

        public OrganizationController(OrganizationService service)
        {
            _service = service;
        }

        [HttpPost("create/{organisationId:int?}")]
        public async Task<IActionResult> CreateOrganization(
            [FromBody] CreateOrganizationRequest request,
            int? organisationId
        )
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _service.CreateOrUpdateAsync(request, organisationId);

            if (created == null)
                return BadRequest($"Organization name '{request.Name}' already exists.");

            return CreatedAtAction(nameof(GetById), new { id = created.OrganizationId }, created);
        }

        // GET ALL
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrganizationDto>), 200)]
        public async Task<ActionResult> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        // GET BY ID
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(OrganizationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetById(int id)
        {
            var org = await _service.GetByIdAsync(id);
            if (org == null) return NotFound($"Organization with ID {id} not found.");

            return Ok(org);
        }

        // CREATE
        //[HttpPost]
        //[ProducesResponseType(typeof(OrganizationDto), 201)]
        //[ProducesResponseType(400)]
        //public async Task<ActionResult> Create([FromBody] OrganizationCreateRequest request)
        //{
        //    var created = await _service.CreateAsync(request);

        //    if (created == null)
        //        return BadRequest($"Organization name '{request.Name}' already exists.");

        //    return CreatedAtAction(nameof(GetById), new { id = created.OrganizationId }, created);
        //}


        // UPDATE
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(OrganizationDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Update(int id, [FromBody] OrganizationUpdateRequest request)
        {
            var updated = await _service.UpdateAsync(id, request);

            if (updated == null) return NotFound($"Organization with ID {id} not found.");

            return Ok(updated);
        }

        // DELETE
        [HttpDelete("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted) return NotFound($"Organization with ID {id} not found.");

            return NoContent();
        }

        [HttpPut("{id}/upload-logo")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UploadLogo(int id, [FromForm] UploadLogoRequest request)
        {
            if (request.File == null)
                return BadRequest("No file provided.");

            var result = await _service.UploadLogoAsyncv2(id, request.File);

            if (result == null)
                return NotFound($"Organization with ID {id} not found.");

            return Ok(new { logoUrl = result });
        }


    }
}
