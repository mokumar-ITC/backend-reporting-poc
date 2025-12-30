using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class UserTrackingResponse
    {
        public int Id { get; set; }

        public string? UserName { get; set; }

        public string? WorkspaceId { get; set; }

        public string? ReportId { get; set; }

        public string? ActionName { get; set; }

        public DateTime OccurredOn { get; set; }
        
    }
}
