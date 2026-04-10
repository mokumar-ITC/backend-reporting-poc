using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
namespace BIEmbedSystem.Services
{

    public class LakehouseTableService
    {
        private readonly string _connectionString;
        private readonly ILogger<LakehouseTableService> _logger;
        private readonly AzureAdSettings _azureAd;
        private readonly string tenantId = "";
        private readonly string clientId = "";
        private readonly string clientSecret = "";
        private readonly string serverAddress = "";
        private readonly string LakehouseName = "";
        /// <summary>
        /// Initializes a new instance of the LakehouseTableService.
        /// </summary>
        /// <param name="configuration">The application's configuration, used to retrieve connection strings.</param>
        /// <param name="logger">The logger for this service.</param>
        public LakehouseTableService(IOptions<AzureAdSettings> azureAdOptions, IConfiguration configuration, ILogger<LakehouseTableService> logger)
        {
            _logger = logger;
            _azureAd = azureAdOptions.Value;
            tenantId = _azureAd.TenantId;
            clientId = _azureAd.ClientId;
            clientSecret = _azureAd.ClientSecret;
            serverAddress = _azureAd.serverAddress;
            LakehouseName = _azureAd.LakeHouseName;


            string ConnectionStringSP = $"Server={serverAddress}; Authentication=Active Directory Service Principal; Encrypt=True; " +
                $"Database={LakehouseName};User Id={clientId}; Password={clientSecret}"; // eightfive_lakehouse - for a lakehouse, eightfive_warehouse - for a DW

            // Retrieve the connection string from appsettings.json
            _connectionString = ConnectionStringSP;//configuration.GetConnectionString("FabricSqlAnalytics");

            // Basic validation to ensure the connection string is present
            if (string.IsNullOrEmpty(_connectionString))
            {
                _logger.LogCritical("Fabric SQL Analytics connection string 'FabricSqlAnalytics' is missing in configuration.");
                throw new InvalidOperationException("Fabric SQL Analytics connection string 'FabricSqlAnalytics' not found in configuration.");
            }
        }

        /// <summary>
        /// Retrieves data from a specified table in the Lakehouse SQL Analytics Endpoint.
        /// </summary>
        /// <param name="tableName">The name of the table to retrieve data from.</param>
        /// <param name="topNRows">Optional: The maximum number of rows to retrieve (defaults to 100).</param>
        /// <returns>A DataTable containing the retrieved data.</returns>
        /// <exception cref="ApplicationException">Thrown if a database-related error occurs.</exception>
        public async Task<DataTable> GetTableDataAsync(string tableName, int topNRows = 100)
        {
            // Log the attempt to fetch data
            _logger.LogInformation($"Attempting to fetch data from table '{tableName}' (TOP {topNRows} rows).");
            DataTable dataTable = new DataTable();

            try
            {
                // Establish a SQL connection using the retrieved connection string.
                // For Azure AD authentication types, Microsoft.Data.SqlClient will automatically handle
                // token acquisition based on the environment (e.g., Managed Identity or logged-in user).
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {

                    //connection.AccessToken = await GetAadAccessTokenForSqlAsync(tenantId, clientId, clientSecret);
                    //await connection.OpenAsync();
                    // ... rest of your code

                    await connection.OpenAsync(); // Open the database connection

                    // Construct the SQL query. Parameterized queries are crucial for security (SQL injection prevention).
                    // Using square brackets around table name in case it contains special characters or spaces.
                    string query = $"SELECT TOP (@topNRows) * FROM [{tableName}]";
                    _logger.LogDebug($"Executing SQL query: {query}");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameter for TOP N rows
                        command.Parameters.AddWithValue("@topNRows", topNRows);

                        // Execute the query and load the results into a DataTable
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader); // Populates the DataTable with query results
                        }
                    }
                }
                // Log successful data retrieval
                _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} rows from table '{tableName}'.");
            }
            catch (SqlException ex)
            {
                // Catch specific SQL exceptions and log them
                _logger.LogError(ex, $"SQL Error accessing table '{tableName}': {ex.Message}");
                // Re-throw as a generic application exception for the controller to handle
                throw new ApplicationException($"Database error accessing table '{tableName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                _logger.LogError(ex, $"An unexpected error occurred while reading table '{tableName}'.");
                throw new ApplicationException($"An unexpected error occurred while reading table '{tableName}'.", ex);
            }
            return dataTable;
        }
        public async Task<DataTable> GetTableDataByQueryAsync(string tableName, string column, int topNRows = 100)
        {
            // Log the attempt to fetch data
            _logger.LogInformation($"Attempting to fetch data from tableName '{tableName}'column {column}  (TOP {topNRows} rows).");
            DataTable dataTable = new DataTable();

            try
            {
                // Establish a SQL connection using the retrieved connection string.
                // For Azure AD authentication types, Microsoft.Data.SqlClient will automatically handle
                // token acquisition based on the environment (e.g., Managed Identity or logged-in user).
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {

                    //connection.AccessToken = await GetAadAccessTokenForSqlAsync(tenantId, clientId, clientSecret);
                    //await connection.OpenAsync();
                    // ... rest of your code

                    await connection.OpenAsync(); // Open the database connection

                    // Construct the SQL query. Parameterized queries are crucial for security (SQL injection prevention).
                    // Using square brackets around table name in case it contains special characters or spaces.
                    string query = $"SELECT TOP (@topNRows) {column} FROM [{tableName}]";
                    _logger.LogDebug($"Executing SQL query: {query}");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameter for TOP N rows
                        command.Parameters.AddWithValue("@topNRows", topNRows);

                        // Execute the query and load the results into a DataTable
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader); // Populates the DataTable with query results
                        }
                    }
                }
                // Log successful data retrieval
                _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} rows from table '{tableName}'.");
            }
            catch (SqlException ex)
            {
                // Catch specific SQL exceptions and log them
                _logger.LogError(ex, $"SQL Error accessing table '{tableName}': {ex.Message}");
                // Re-throw as a generic application exception for the controller to handle
                throw new ApplicationException($"Database error accessing table '{tableName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                _logger.LogError(ex, $"An unexpected error occurred while reading table '{tableName}'.");
                throw new ApplicationException($"An unexpected error occurred while reading table '{tableName}'.", ex);
            }
            return dataTable;
        }

        public async Task<DataTable> GetTableAsync()
        {
            // Log the attempt to fetch data
            _logger.LogInformation($"Attempting to fetch table Name.");
            DataTable dataTable = new DataTable();

            try
            {
                // Establish a SQL connection using the retrieved connection string.
                // For Azure AD authentication types, Microsoft.Data.SqlClient will automatically handle
                // token acquisition based on the environment (e.g., Managed Identity or logged-in user).
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {

                    //connection.AccessToken = await GetAadAccessTokenForSqlAsync(tenantId, clientId, clientSecret);
                    //await connection.OpenAsync();
                    // ... rest of your code

                    await connection.OpenAsync(); // Open the database connection

                    // Construct the SQL query. Parameterized queries are crucial for security (SQL injection prevention).
                    // Using square brackets around table name in case it contains special characters or spaces.
                    string query = $" SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                    _logger.LogDebug($"Executing SQL query: {query}");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Execute the query and load the results into a DataTable
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader); // Populates the DataTable with query results
                        }
                    }
                }
                // Log successful data retrieval
                _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} rows table.");
            }
            catch (SqlException ex)
            {
                // Catch specific SQL exceptions and log them
                _logger.LogError(ex, $"SQL Error accessing base table : {ex.Message}");
                // Re-throw as a generic application exception for the controller to handle
                throw new ApplicationException($"Database error accessing base table : {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                _logger.LogError(ex, $"An unexpected error occurred while reading base table .");
                throw new ApplicationException($"An unexpected error occurred while reading base table.", ex);
            }
            return dataTable;
        }

        public async Task<DataTable> GetTableColumnAsync(string tableName)
        {
            // Log the attempt to fetch data
            _logger.LogInformation($"Attempting to fetch table Column.");
            DataTable dataTable = new DataTable();

            try
            {
                // Establish a SQL connection using the retrieved connection string.
                // For Azure AD authentication types, Microsoft.Data.SqlClient will automatically handle
                // token acquisition based on the environment (e.g., Managed Identity or logged-in user).
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {

                    //connection.AccessToken = await GetAadAccessTokenForSqlAsync(tenantId, clientId, clientSecret);
                    //await connection.OpenAsync();
                    // ... rest of your code

                    await connection.OpenAsync(); // Open the database connection

                    // Construct the SQL query. Parameterized queries are crucial for security (SQL injection prevention).
                    // Using square brackets around table name in case it contains special characters or spaces.
                    string query = $"SELECT COLUMN_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
                    _logger.LogDebug($"Executing SQL query: {query}");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Execute the query and load the results into a DataTable
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader); // Populates the DataTable with query results
                        }
                    }
                }
                // Log successful data retrieval
                _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} columns table.");
            }
            catch (SqlException ex)
            {
                // Catch specific SQL exceptions and log them
                _logger.LogError(ex, $"SQL Error accessing columns table : {ex.Message}");
                // Re-throw as a generic application exception for the controller to handle
                throw new ApplicationException($"Database error accessing columns table : {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Catch any other unexpected exceptions
                _logger.LogError(ex, $"An unexpected error occurred while reading columns table .");
                throw new ApplicationException($"An unexpected error occurred while reading columns table.", ex);
            }
            return dataTable;
        }

        /// <summary>
        /// Retrieves filtered data from a specified table in the Lakehouse SQL Analytics Endpoint.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="columnName">The column to filter by.</param>
        /// <param name="filterValue">The value to filter for.</param>
        /// <returns>A DataTable containing the filtered data.</returns>
        /// <exception cref="ApplicationException">Thrown if a database-related error occurs.</exception>
        public async Task<DataTable> GetFilteredTableDataAsync(string tableName, string columnName, string filterValue)
        {
            _logger.LogInformation($"Fetching filtered data from table '{tableName}' where [{columnName}] = '{filterValue}'.");
            DataTable dataTable = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // IMPORTANT: Always use parameterized queries for dynamic filters to prevent SQL Injection!
                    string query = $"SELECT * FROM [{tableName}] WHERE [{columnName}] = @filterValue";
                    _logger.LogDebug($"Executing parameterized SQL query: {query}");

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        // Add parameter for the filter value
                        command.Parameters.AddWithValue("@filterValue", filterValue);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            dataTable.Load(reader);
                        }
                    }
                }
                _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} filtered rows from table '{tableName}'.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, $"SQL Error accessing filtered table '{tableName}': {ex.Message}");
                throw new ApplicationException($"Database error accessing table '{tableName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An unexpected error occurred while reading filtered table '{tableName}'.");
                throw new ApplicationException($"An unexpected error occurred while reading filtered table '{tableName}'.", ex);
            }
            return dataTable;
        }

        /// <summary>
        /// Converts a DataTable to a list of dynamic objects for JSON serialization.
        /// </summary>
        /// <param name="dt">The DataTable to convert.</param>
        /// <returns>A list of dynamic objects.</returns>
        public static List<dynamic> ToDynamicList(DataTable dt)
        {
            var dynamicList = new List<dynamic>();
            foreach (DataRow row in dt.Rows)
            {
                var dynamicObject = new System.Dynamic.ExpandoObject() as IDictionary<string, Object>;
                foreach (DataColumn col in dt.Columns)
                {
                    dynamicObject.Add(col.ColumnName, row[col]);
                }
                dynamicList.Add(dynamicObject);
            }
            return dynamicList;
        }

        public async Task<string> GetAadAccessTokenForSqlAsync(string tenantId, string clientId, string clientSecret)
        {
            var app = ConfidentialClientApplicationBuilder.Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
                .Build();

            // Scope for Azure SQL Database / Fabric SQL Analytics Endpoint
            string[] scopes = new[] { "https://database.windows.net/.default" };

            AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
            return result.AccessToken;
        }

        // Then in your LakehouseTableService.cs where you open the connection:
        public async Task<List<Dictionary<string, object?>>> ExecuteRawQueryAsync(string lakehouseName, string sql)
        {
            _logger.LogInformation("Executing raw query on lakehouse '{LakehouseName}'", lakehouseName);

            // Safety check — only allow SELECT
            if (!sql.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected non-SELECT query on lakehouse '{LakehouseName}': {Sql}", lakehouseName, sql);
                throw new ApplicationException("Only SELECT queries are permitted.");
            }

            string connectionString = BuildConnectionString(lakehouseName);
            var results = new List<Dictionary<string, object?>>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    _logger.LogInformation("Connection opened to lakehouse '{LakehouseName}'", lakehouseName);

                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        command.CommandTimeout = 120;

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new Dictionary<string, object?>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    var columnName = reader.GetName(i);
                                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                                    // ✅ Handle types that don't serialize well to JSON
                                    row[columnName] = value switch
                                    {
                                        DateTime dt => dt.ToString("yyyy-MM-ddTHH:mm:ss"),
                                        DateTimeOffset dto => dto.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                                        byte[] bytes => Convert.ToBase64String(bytes),
                                        Guid g => g.ToString(),
                                        _ => value
                                    };
                                }

                                results.Add(row);
                            }
                        }
                    }
                }

                _logger.LogInformation(
                    "ExecuteRawQueryAsync completed for lakehouse '{LakehouseName}'. Rows returned: {Count}",
                    lakehouseName, results.Count);

                return results;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error executing raw query on lakehouse '{LakehouseName}'. Query: {Sql}",
                    lakehouseName, sql);
                throw new ApplicationException($"SQL error on lakehouse '{lakehouseName}': {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error executing raw query on lakehouse '{LakehouseName}'.",
                    lakehouseName);
                throw;
            }
        }


        public async Task<DataTable> GetTableDataByLakehouseAsync(string lakehouseName, string tableName, int topN = 100)
        {
            // ✅ Build connection string dynamically from lakehouse name
            string connectionString = BuildConnectionString(lakehouseName);

            if (string.IsNullOrEmpty(connectionString))
                throw new ApplicationException($"Could not build connection string for lakehouse '{lakehouseName}'.");

            return await GetTableDataWithConnectionAsync(connectionString, tableName, topN);
        }

        // ✅ Builds the Fabric Lakehouse ODBC/JDBC connection string from lakehouse name
        private string BuildConnectionString(string _lakehouseName)
        {
            return $"Server={serverAddress}; Authentication=Active Directory Service Principal; Encrypt=True; " +
                $"Database={_lakehouseName};User Id={clientId}; Password={clientSecret}"; // eightfive_lakehouse - for a lakehouse, eightfive_warehouse - for a DW
        }

        // ✅ Core data fetch using the built connection string
        private async Task<DataTable> GetTableDataWithConnectionAsync(string connectionString, string tableName, int topN)
        {
            // Sanitize tableName to prevent SQL injection
            if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ApplicationException($"Invalid table name format: '{tableName}'.");

            var dataTable = new DataTable();

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = $"SELECT TOP {topN} * FROM [{tableName}]";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 120; // Fabric can be slow on cold start

            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);

            return dataTable;
        }

        public async Task<DataTable> GetTableColumnByLakehouseAsync(string lakehouseName, string tableName)
        {
            // ✅ Build connection string dynamically from lakehouse name
            string connectionString = BuildConnectionString(lakehouseName);
            if (string.IsNullOrEmpty(connectionString))
                throw new ApplicationException($"Could not build connection string for lakehouse '{lakehouseName}'.");
            return await GetTableColumnWithConnectionAsync(connectionString, tableName);
        }

        public async Task<DataTable> GetTableColumnWithConnectionAsync(string connectionString, string tableName)
        {
            // Sanitize tableName to prevent SQL injection
            if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$"))
                throw new ApplicationException($"Invalid table name format: '{tableName}'.");
            var dataTable = new DataTable();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var query = $"SELECT COLUMN_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 120; // Fabric can be slow on cold start
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            return dataTable;
        }

        
        public async Task<DataTable> GetLakehouseTableColumnNamesAsync(string lakehouseName, string tableName)
        {
            // ✅ Build connection string dynamically from lakehouse name
            string connectionString = BuildConnectionString(lakehouseName);
            if (string.IsNullOrEmpty(connectionString))
                throw new ApplicationException($"Could not build connection string for lakehouse '{lakehouseName}'.");
            var dataTable = new DataTable();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            var query = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";
            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 120; // Fabric can be slow on cold start
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(dataTable);
            return dataTable;
        }

        public async Task<DataTable> GetTablesByLakehouseAsync(string lakehouseName)
        {
            string connectionString = BuildConnectionString(lakehouseName);
            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {
                await sqlConnection.OpenAsync(); // Open the database connection

                // Construct the SQL query. Parameterized queries are crucial for security (SQL injection prevention).
                // Using square brackets around table name in case it contains special characters or spaces.
                string query = $" SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                _logger.LogDebug($"Executing SQL query: {query}");

                using (SqlCommand command = new SqlCommand(query, sqlConnection))
                {
                    // Execute the query and load the results into a DataTable
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        dataTable.Load(reader); // Populates the DataTable with query results
                    }
                }
            }
            // Log successful data retrieval
            _logger.LogInformation($"Successfully fetched {dataTable.Rows.Count} rows table.");
            return dataTable;
        }
    }
}
