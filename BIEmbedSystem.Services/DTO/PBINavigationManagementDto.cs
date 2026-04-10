using BIEmbedSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class LakehouseTableDto
    {
        public string TableName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
    }

    public class LakehouseConfigDto
    {
        public string Lakehouse { get; set; } = string.Empty;
        public List<LakehouseTableDto> Tables { get; set; } = new();
    }

    public class PBINavigationManagementDto : PBINavigationManagement
    {
        // Deserialised version of LakehouseConfig string
        public LakehouseConfigDto? LakehouseConfigMapped { get; set; }
    }
}
