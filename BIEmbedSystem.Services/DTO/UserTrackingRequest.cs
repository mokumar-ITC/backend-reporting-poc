using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class UserTrackingRequest
    {
        public int UserId { get; set; }

        public int OrganizationId { get; set; }
        public string WorkspaceId { get; set; } = "";
        public string ReportId { get; set; } = "";
        public string ActionName { get; set; } = "";
        public string? ActionDescription { get; set; }
    }
}
