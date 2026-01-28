using Azure.Core;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using BIEmbedSystem.Core.Entities;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class OneLakeService
    {
        private readonly DataLakeServiceClient _dataLakeServiceClient;
        private readonly AzureAdSettings _azureAd;

        public OneLakeService(IOptions<AzureAdSettings> azureAdOptions)
        {
            // Authenticate using DefaultAzureCredential
            // This will try various methods to get a token (e.g., AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_CLIENT_SECRET environment variables,
            // Azure CLI login, Visual Studio login, Managed Identity if deployed on Azure)
            _azureAd = azureAdOptions.Value;
            var tenantId = _azureAd.TenantId;
            var clientId = _azureAd.ClientId;
            var clientSecret = _azureAd.ClientSecret;

            TokenCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            // TokenCredential credential = new DefaultAzureCredential();

            // The account name for OneLake is always "onelake"
            string accountUrl = "https://onelake.dfs.fabric.microsoft.com";


            _dataLakeServiceClient = new DataLakeServiceClient(new Uri(accountUrl), credential);
        }
        public async Task<List<string>> ListWorkspacesAsync()
        {
            List<string> workspaces = new List<string>();
            await foreach (var fileSystem in _dataLakeServiceClient.GetFileSystemsAsync())
            {
                workspaces.Add(fileSystem.Name);
            }
            return workspaces;
        }
        public async Task<List<string>> ListLakehouseItemsAsync(string workspaceName, string lakehouseName)
        {
            DataLakeFileSystemClient fileSystemClient = _dataLakeServiceClient.GetFileSystemClient(workspaceName);
            DataLakeDirectoryClient directoryClient = fileSystemClient.GetDirectoryClient($"{lakehouseName}.Lakehouse"); // Note the .Lakehouse extension

            List<string> items = new List<string>();
            await foreach (var pathItem in directoryClient.GetPathsAsync())
            {
                items.Add(pathItem.Name);
            }
            return items;
        }
        public async Task<string> ReadFileContentAsync(string workspaceName, string lakehouseName, string filePath)
        {
            DataLakeFileSystemClient fileSystemClient = _dataLakeServiceClient.GetFileSystemClient(workspaceName);
            DataLakeFileClient fileClient = fileSystemClient.GetFileClient($"{lakehouseName}.Lakehouse/Files/{filePath}"); // Assuming file is in 'Files' directory
                                                                                                                           // Construct the path: Lakehouse.<lakehouseName>/Tables/<TableName>/
            string tablePath = $"Lakehouse.{lakehouseName}/Tables/sampledata";
            var directoryClient = fileSystemClient.GetDirectoryClient(tablePath);
            List<string> items = new List<string>();
            await foreach (var pathItem in directoryClient.GetPathsAsync())
            {
                items.Add(pathItem.Name);
            }
            Azure.Response<Azure.Storage.Files.DataLake.Models.FileDownloadInfo> downloadResponse = await fileClient.ReadAsync();
            var listdb = ReadCsvToDataTable(downloadResponse.Value.Content);
            using (StreamReader reader = new StreamReader(downloadResponse.Value.Content))
            {
                 return await reader.ReadToEndAsync();
                //string jsonContent = await reader.ReadToEndAsync();
                //var jsonObject = JsonSerializer.Deserialize<object>(jsonContent); // or use Newtonsoft.Json
                //return jsonObject; // ASP.NET Core automatically returns JSON
            }
        
        }

        //public async Task<DataLakeFileSystemClient> GetLakehouseFileSystemClient(Guid workspaceId)
        //{
        //    try
        //    {
        //        // Workspace GUIDs are often used for file system names in Fabric for direct DFS access
        //        var fileSystemClient = _dataLakeServiceClient.GetFileSystemClient(workspaceId.ToString());
        //        // You might need to call GetPropertiesAsync or ExistsAsync to ensure it's valid
        //        await fileSystemClient.GetPropertiesAsync();
        //        //_logger.LogDebug($"Got FileSystemClient for workspace ID: {workspaceId}");
        //        return fileSystemClient;
        //    }
        //    catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        //    {
        //        //_logger.LogError($"Workspace ID '{workspaceId}' not found as a file system.");
        //        throw new DirectoryNotFoundException($"Workspace ID '{workspaceId}' not found as a file system.", ex);
        //    }
        //    catch (Exception ex)
        //    {
        //        //_logger.LogError(ex, $"Error getting file system client for workspace ID: {workspaceId}");
        //        throw;
        //    }
        //}
        //// Example of getting a directory client for the table's root
        //public async Task<DataLakeDirectoryClient> GetTableDirectoryClient(Guid workspaceId, string lakehouseName, string tableName)
        //{
        //    var fileSystemClient = await GetLakehouseFileSystemClient(workspaceId);
        //    // Construct the path: Lakehouse.<lakehouseName>/Tables/<TableName>/
        //    string tablePath = $"Lakehouse.{lakehouseName}/Tables/{tableName}";
        //    var directoryClient = fileSystemClient.GetDirectoryClient(tablePath);
        //    //_logger.LogDebug($"Got DirectoryClient for table path: {tablePath}");
        //    return directoryClient;
        //}

        public async Task UploadFileContentAsync(string workspaceName, string lakehouseName, string filePath, string content)
        {
            DataLakeFileSystemClient fileSystemClient = _dataLakeServiceClient.GetFileSystemClient(workspaceName);
            DataLakeFileClient fileClient = fileSystemClient.GetFileClient($"{lakehouseName}.Lakehouse/Files/{filePath}"); // Assuming file is in 'Files' directory

            using (MemoryStream stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content)))
            {
                await fileClient.UploadAsync(stream, overwrite: true); // overwrite: true allows updating existing file
            }
        }

        //public async Task<string> ReadFileContentAsync(string workspaceName, string lakehouseName, string filePath)
        //{
        //    DataLakeFileSystemClient fileSystemClient = _dataLakeServiceClient.GetFileSystemClient(workspaceName);
        //    DataLakeFileClient fileClient = fileSystemClient.GetFileClient($"{lakehouseName}.Lakehouse/Files/{filePath}"); // Assuming file is in 'Files' directory

        //    Azure.Response<Azure.Storage.Files.DataLake.Models.FileDownloadInfo> downloadResponse = await fileClient.ReadAsync();
        //    using (StreamReader reader = new StreamReader(downloadResponse.Value.Content))
        //    {
        //        return await reader.ReadToEndAsync();
        //    }
        //}
        public async Task<DataTable> ReadCsvToDataTable(Stream contentStream)
        {
            using (var reader = new StreamReader(contentStream, Encoding.UTF8))
            {
                var dataTable = new DataTable();
                bool isHeader = true;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) continue;

                    var values = line.Split(',');

                    if (isHeader)
                    {
                        foreach (var header in values)
                        {
                            dataTable.Columns.Add(header.Trim());
                        }
                        isHeader = false;
                    }
                    else
                    {
                        dataTable.Rows.Add(values);
                    }
                }

                return dataTable;
            }
        }

    }

}
