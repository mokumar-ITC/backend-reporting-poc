using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class SemanticSchedulerCreateDto
    {
        public Guid WorkspaceId { get; set; }
        public Guid DatasetId { get; set; }

        public string SchedulerName { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsIncrementalRefresh { get; set; }
        public string? RefreshTables { get; set; }

        public DateTime ScheduleStartDate { get; set; }
        public DateTime? ScheduleEndDate { get; set; }

        public string RepeatType { get; set; } = "Daily";

        public int ScheduleHour { get; set; }
        public int ScheduleMinute { get; set; }
        public string ScheduleAMPM { get; set; } = "AM";

        public string TimeZone { get; set; } = "UTC";
    }

}
