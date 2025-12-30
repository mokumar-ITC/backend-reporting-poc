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
        public string Domain { get; set; } = "";
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }

        public string LogoUrl { get; set; } = "";
    }

}
