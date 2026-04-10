using Asp.Versioning;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AiAgentController : Controller
    {
        private readonly IAiAgentService _service;

        public AiAgentController(IAiAgentService service)
        {
            _service = service;
        }

        //[HttpPost("create")]
        //public async Task<IActionResult> Create(CreateAgentDto dto)
        //{
        //    var result = await _service.CreateAgentAsync(dto);
        //    return Ok(result);
        //}

        //[HttpPut("update")]
        //public async Task<IActionResult> Update(UpdateAgentDto dto)
        //{
        //    var result = await _service.UpdateAgentAsync(dto);
        //    return Ok(result);
        //}

        //[HttpGet("check")]
        //public async Task<IActionResult> Check(CheckAgentDto dto)
        //{
        //    var result = await _service.CheckAgentAsync(dto);
        //    return Ok(result);
        //}

        //[HttpDelete("{id}")]
        //public async Task<IActionResult> Delete(Guid id)
        //{
        //    await _service.DeleteAgentAsync(id);
        //    return Ok("Deleted successfully");
        //}

        [HttpPost("query")]
        [ProducesResponseType(typeof(AiQueryResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Query([FromBody] AiQueryRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ReportId))
                return BadRequest("ReportId is required.");

            if (string.IsNullOrWhiteSpace(dto.UserQuery))
                return BadRequest("UserQuery cannot be empty.");

            try
            {
                var result = await _service.QueryAsync(dto);

                if (!result.Success)
                    return StatusCode(StatusCodes.Status500InternalServerError, result.ErrorMessage);

                return Ok(result);
            }
            catch (ApplicationException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }
    }
}
