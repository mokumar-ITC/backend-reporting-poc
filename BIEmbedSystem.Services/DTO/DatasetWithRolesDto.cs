using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{

    public class DatasetWithRolesDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<RoleRLSDto> Roles { get; set; } = new();
    }
    public class RoleRLSDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

}
