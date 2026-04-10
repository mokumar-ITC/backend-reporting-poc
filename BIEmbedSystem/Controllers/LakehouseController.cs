// Controllers/LakehouseController.cs
using Asp.Versioning;
using BIEmbedSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataPlexus.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/fabric-lakehouse")]
    public class LakehouseController : ControllerBase
    {
        private readonly LakehouseTableService _lakehouseTableService;
        private readonly ILogger<LakehouseController> _logger;

        public LakehouseController(ILogger<LakehouseController> logger, LakehouseTableService lakehouseTableService)
        {
            //_tableDataReader = tableDataReader;
            _logger = logger;
            _lakehouseTableService = lakehouseTableService;
        }
        /// <summary>
        /// Retrieves all (or TOP N) rows from a specified table in the Fabric Lakehouse using Service Principal authentication.
        /// Example: GET /api/fabric-lakehouse/table/YourTableName?topN=50
        /// </summary>
        /// <param name="tableName">The name of the table to retrieve data from.</param>
        /// <param name="topN">Optional: The maximum number of rows to retrieve (defaults to 100).</param>
        /// <returns>A list of dynamic objects representing the table rows.</returns>
        [HttpGet("getTable")]
        [ProducesResponseType(typeof(List<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<dynamic>>> GetTable()
        {
            try
            {
                _logger.LogInformation($"API Call: GetTable for table via Service Principal.");
                var data = await _lakehouseTableService.GetTableAsync();//_tableDataReader.GetTableDataAsync(tableName, topN);
                return Ok(LakehouseTableService.ToDynamicList(data));
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"Application error retrieving data from table .");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while processing.'.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving table data.");
            }
        }
        /// <summary>
        /// //Example: GET /api/fabric-lakehouse/table/YourTableName
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        [HttpGet("GetTableColumn")]
        [ProducesResponseType(typeof(List<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<dynamic>>> GetTableColumn(string tableName)
        {

            try
            {
                _logger.LogInformation($"API Call: GetTableColumn for table '{tableName} via Service Principal.");
                var data = await _lakehouseTableService.GetTableColumnAsync(tableName);//_tableDataReader.GetTableDataAsync(tableName, topN);
                return Ok(LakehouseTableService.ToDynamicList(data));
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"Application error retrieving data from table .");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while processing.'.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving table data.");
            }
        }




        /// <summary>
        /// Retrieves all (or TOP N) rows from a specified table in the Fabric Lakehouse using Service Principal authentication.
        /// Example: GET /api/fabric-lakehouse/table/YourTableName?topN=50
        /// </summary>
        /// <param name="tableName">The name of the table to retrieve data from.</param>
        /// <param name="topN">Optional: The maximum number of rows to retrieve (defaults to 100).</param>
        /// <returns>A list of dynamic objects representing the table rows.</returns>
        [HttpGet("table/{tableName}")]
        [ProducesResponseType(typeof(List<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<dynamic>>> GetTableData(string tableName, [FromQuery] int topN = 100)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogWarning("GetTableData request: Table name is null or empty.");
                return BadRequest("Table name cannot be empty.");
            }
            if (topN <= 0)
            {
                _logger.LogWarning($"GetTableData request: Invalid topN value {topN}. Must be greater than 0.");
                return BadRequest("topN parameter must be greater than 0.");
            }

            try
            {
                _logger.LogInformation($"API Call: GetTableData for table '{tableName}' (TOP {topN}) via Service Principal.");
                var data = await _lakehouseTableService.GetTableDataAsync(tableName, topN);//_tableDataReader.GetTableDataAsync(tableName, topN);
                return Ok(LakehouseTableService.ToDynamicList(data));
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"Application error retrieving data from table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while processing request for table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving table data.");
            }
        }

        [HttpGet("lakehouse/{lakehouseName}/table/{tableName}/getdata")]
        [ProducesResponseType(typeof(List<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<dynamic>>> GetLakehouseTableData(
        string lakehouseName,
        string tableName,
        [FromQuery] int topN = 100)
        {
            if (string.IsNullOrWhiteSpace(lakehouseName))
            {
                _logger.LogWarning("GetLakehouseTableData: Lakehouse name is null or empty.");
                return BadRequest("Lakehouse name cannot be empty.");
            }

            if (string.IsNullOrWhiteSpace(tableName))
            {
                _logger.LogWarning("GetLakehouseTableData: Table name is null or empty.");
                return BadRequest("Table name cannot be empty.");
            }

            if (topN <= 0)
            {
                _logger.LogWarning($"GetLakehouseTableData: Invalid topN value {topN}. Must be greater than 0.");
                return BadRequest("topN parameter must be greater than 0.");
            }

            try
            {
                _logger.LogInformation($"API Call: GetLakehouseTableData for lakehouse '{lakehouseName}', table '{tableName}' (TOP {topN}).");

                var data = await _lakehouseTableService.GetTableDataByLakehouseAsync(lakehouseName, tableName, topN);
                return Ok(LakehouseTableService.ToDynamicList(data));
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, $"Application error retrieving data from lakehouse '{lakehouseName}', table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error for lakehouse '{lakehouseName}', table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving table data.");
            }
        }
        /// <summary>
        /// Retrieves filtered rows from a specified table based on a column and filter value.
        /// Example: GET /api/onelake/table/YourTableName/filter?columnName=YourColumn&filterValue=YourValue
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnName">The column to filter by.</param>
        /// <param name="filterValue">The value to filter for.</param>
        /// <returns>A list of dynamic objects representing the filtered rows.</returns>
        [HttpGet("table/{tableName}/filter")]
        [ProducesResponseType(typeof(List<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<dynamic>>> GetFilteredTableData(
        string tableName,
        [FromQuery]
        string columnName,
        [FromQuery]
        string filterValue)
        {
            // Input validation for required parameters
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(columnName) || string.IsNullOrWhiteSpace(filterValue))
            {
                _logger.LogWarning("GetFilteredTableData request: Missing required parameters (tableName, columnName, or filterValue).");
                return BadRequest("Table name, column name, and filter value are required for filtering.");
            }
            try
            {
                _logger.LogInformation($"API Call: GetFilteredTableData for table '{tableName}' where [{columnName}] = '{filterValue}'.");
                DataTable data = await _lakehouseTableService.GetFilteredTableDataAsync(tableName, columnName, filterValue);
                // Convert DataTable to a list of dynamic objects for clean JSON serialization
                return Ok(LakehouseTableService.ToDynamicList(data));
            }
            catch (ApplicationException ex)
            {
                // Log and return internal server error for database-related issues
                _logger.LogError(ex, $"Application error retrieving filtered data from table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                _logger.LogError(ex, $"An unexpected error occurred while processing filtered request for table '{tableName}'.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while retrieving filtered table data.");
            }
        }

        [HttpGet("lakehouse/{lakehouseName}/tables")]
        public async Task<IActionResult> GetTablesAsync(string lakehouseName)
        {
            if (string.IsNullOrEmpty(lakehouseName))
                return BadRequest("Lakehouse name is required.");

            try
            {
                var tables = await _lakehouseTableService.GetTablesByLakehouseAsync(lakehouseName);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("lakehouse-table-column/{lakehouseName}/table/{tableName}/column-names")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetColumnNamesOnlyAsync(string lakehouseName, string tableName)
        {
            if (string.IsNullOrEmpty(lakehouseName))
                return BadRequest("Lakehouse name is required.");
            if (string.IsNullOrEmpty(tableName))
                return BadRequest("Table name is required.");

            try
            {
                // Call the service method that returns List<string>
                var columnNames = await _lakehouseTableService.GetLakehouseTableColumnNamesAsync(lakehouseName, tableName);

                // Return the simple list directly
                return Ok(columnNames);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetColumnNamesOnlyAsync");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}

