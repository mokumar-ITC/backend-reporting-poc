using Asp.Versioning;
using Azure.Storage.Files.DataLake;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class OneLakeController : ControllerBase
    {
        private readonly ILogger<OneLakeController> _logger;
        private readonly OneLakeService _oneLakeService;

        public OneLakeController(ILogger<OneLakeController> logger, OneLakeService oneLakeService)
        {
            _logger = logger;
            _oneLakeService = oneLakeService;
        }
        [HttpGet(Name = "GetOneLake")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> Get()
        { 
        return Ok("This is the OneLake API Version 1.0");
        }
      

        [HttpGet("workspaces")]
        public async Task<ActionResult<List<string>>> GetWorkspaces()
        {
            var workspaces = await _oneLakeService.ListWorkspacesAsync();
            return Ok(workspaces);
        }

        [HttpGet("lakehouse-items")]
        public async Task<ActionResult<List<string>>> GetLakehouseItems(string workspaceName, string lakehouseName)
        {
            var items = await _oneLakeService.ListLakehouseItemsAsync(workspaceName, lakehouseName);
            return Ok(items);
        }

        [HttpGet("read-file")]
        public async Task<ActionResult<string>> ReadFile(string workspaceName, string lakehouseName, string filePath)
        {
            try
            {
                var content = await _oneLakeService.ReadFileContentAsync(workspaceName, lakehouseName, filePath);
                return Ok(content);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error reading file: {ex.Message}");
            }
        }
       

        [HttpPost("upload-file")]
        public async Task<ActionResult> UploadFile(string workspaceName, string lakehouseName, string filePath, [FromBody] string content)
        {
            try
            {
                await _oneLakeService.UploadFileContentAsync(workspaceName, lakehouseName, filePath, content);
                return Ok("File uploaded successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error uploading file: {ex.Message}");
            }
        }
    }
}
