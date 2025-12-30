using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class BulkNavigationUserAccessUpdateRequest
    {
        public List<int> UserIds { get; set; } = new();

        public int NagivationId { get; set; }

        // Permissions
        public bool? ShowDatasetPane { get; set; }
        public bool? ShowEdit { get; set; }
        public bool? ShowBookmark { get; set; }
        public bool? ShareReport { get; set; }
        public bool? ExportReport { get; set; }
        public bool? ScheduleReport { get; set; }
        public bool? ScheduleSemantic { get; set; }

        public bool? IsActive { get; set; }

        public int OrganizationId { get; set; }
        public string UpdatedBy { get; set; }
    }
}

