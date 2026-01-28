using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class BulkUserUpdateRequest
    {
        public List<int> UserIds { get; set; } = new();
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public int OrganizationId { get; set; }
    }
}
