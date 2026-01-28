using Asp.Versioning;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Rest;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;
     
        private readonly HomeServices _homeServices;
        private readonly AzureGraphService _azureGraphService;


        public HomeController(ILogger<HomeController> logger,  HomeServices homeServices, AzureGraphService azureGraphService)
        {
            _logger = logger;
            _azureGraphService = azureGraphService;
           // _config = config;
            _homeServices = homeServices;
        
        }
     
        [HttpGet("GetHeaderInfo")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult> GetHeaderInfo()
        {
            var result = await _homeServices.GetHeaderInfo();
            _logger.LogInformation("Get API of GetHeaderInfo Version 1");
            return Ok(result);
        }

        [HttpGet("getUserList")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> getUserListAsync()
        {
            try
            {
                var embedParams = await _azureGraphService.GetUserList();
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
        [HttpGet("getRolesList")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> geRolesListAsync()
        {
            try
            {
                var embedParams = await _azureGraphService.GetRolesList();
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        [HttpGet("getRolesListInHouse/{organisationId:int}")]
        [MapToApiVersion("1.0")]
        public async Task<IActionResult> geRolesListInHouseAsync(int organisationId)
        {
            try
            {
                var embedParams = await _azureGraphService.GetRolesByOrganisationAsync(organisationId);
                return Ok(embedParams);
            }
            catch (HttpOperationException exc)
            {
                HttpContext.Response.StatusCode = (int)exc.Response.StatusCode;
                var message = string.Format("Status: {0} ({1})\r\nResponse: {2}\r\nRequestId: {3}", exc.Response.StatusCode, (int)exc.Response.StatusCode, exc.Response.Content, exc.Response.Headers["RequestId"].FirstOrDefault());
                return BadRequest(message);
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                return BadRequest(ex.Message + "\n\n" + ex.StackTrace);
            }
        }
    }
}
