using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class CreateOrganizationRequest
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string DomainUrl { get; set; }

        public string AuthenticationScheme { get; set; }
        public string Language { get; set; }

        public bool IsActive { get; set; }

        public PowerBIConfig PowerBI { get; set; }
    }

    public class PowerBIConfig
    {
        public string CapacityId { get; set; }
        public string WorkspaceId { get; set; }
    }

}
