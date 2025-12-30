using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    public class Header
    {
        public required string Lakehouse {  get; set; }
        public required string UserId { get; set; }
        public required string UserRole { get; set; }
        public required string LevelNo { get; set; }
    }
}
