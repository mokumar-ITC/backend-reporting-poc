
using Asp.Versioning;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
// Controllers/PowerBiController.cs
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{


    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class PowerBiController : ControllerBase
    {
        private readonly IPowerBiService _powerBiService;

        public PowerBiController(IPowerBiService powerBiService)
        {
            _powerBiService = powerBiService;
        }

        [HttpPost("embed")]
        public async Task<IActionResult> GetEmbedToken([FromBody] EmbedRequestDto dto)
        {
            try
            {
                var result = await _powerBiService.GenerateEmbedTokenAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Don't leak secrets in production; return safe error
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

}
