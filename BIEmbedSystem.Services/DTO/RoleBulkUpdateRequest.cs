using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class RoleBulkUpdateRequest
    {
        public List<int> RoleIds { get; set; } = new();
        public bool IsActive { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
