using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class OrganizationDto
    {
        public int OrganizationId { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Language { get; set; } = "";
        public string AuthScheme { get; set; } = "";
        public string EmbeddedCapacityName { get; set; } = "";

        public string WorkpaceId { get; set; } = "";
        public string Domain { get; set; } = "";
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }

        public string LogoUrl { get; set; } = "";
        public string WorkspaceId { get; internal set; }
    }

}
