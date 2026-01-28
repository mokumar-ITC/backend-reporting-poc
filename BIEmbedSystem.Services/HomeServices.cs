using BIEmbedSystem.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class HomeServices
    {
        private readonly string _connectionString;
        private readonly ILogger<LakehouseTableService> _logger;
        private readonly AzureAdSettings _azureAd;
        
        private readonly string tenantId = "";
        private readonly string clientId = "";
        private readonly string clientSecret = "";
        private readonly string serverAddress = "";
        private readonly string LakehouseName = "";
        private readonly string LevelNo = "";
        private readonly string UserRole = "";
        private readonly string UserId = "";
        public HomeServices(IOptions<AzureAdSettings> azureAdOptions,
             IConfiguration configuration, ILogger<LakehouseTableService> logger)
        {
            _logger = logger;
            _azureAd = azureAdOptions.Value;
            tenantId = _azureAd.TenantId;
            clientId = _azureAd.ClientId;
            clientSecret = _azureAd.ClientSecret;
            serverAddress = _azureAd.serverAddress;
            LakehouseName = _azureAd.LakeHouseName;
            //_userRole = roleOptions.Value;
            //LevelNo = _userRole.HardcodedRole.LevelNo.ToString();
            //UserId = _userRole.HardcodedRole.UserName;
            //UserRole = _userRole.HardcodedRole.UserRole;

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
        public async Task<Header> GetHeaderInfo()
        {
            Header header = new()
            {
                Lakehouse = LakehouseName,
                LevelNo = LevelNo,
                UserId = UserId,
                UserRole = UserRole

            };
            return header;
        }
    }
}
