using Asp.Versioning;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/translate")]
    public class TranslationController : ControllerBase
    {
        private readonly ITranslationService _translatorService;

        public TranslationController(ITranslationService translatorService)
        {
            _translatorService = translatorService;
        }

        [HttpPost("sidebar")]
        public async Task<IActionResult> TranslateSidebar(
            [FromBody] TranslateSidebarRequest request
        )
        {
            if (request.Texts == null || request.Texts.Count == 0)
                return BadRequest("Sidebar text list cannot be empty.");

            var result = await _translatorService.TranslateAsync(
                request.Texts,
                request.TargetLanguage
            );

            return Ok(result);
        }

    }
}
