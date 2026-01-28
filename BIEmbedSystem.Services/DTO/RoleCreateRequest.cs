using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class RoleCreateRequest
    {
        public string Name { get; set; } = null!;
        public int OrganizationId { get; set; }
        public int? CreatedBy { get; set; }
    }
}
