using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class QueryRequest
    {
        public string WorkspaceId { get; set; }
        public string AgentId { get; set; }
        public string Question { get; set; }
    }
}
