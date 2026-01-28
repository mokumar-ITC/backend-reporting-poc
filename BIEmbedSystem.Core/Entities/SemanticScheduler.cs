using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("SemanticSchedulers")]
    public class SemanticScheduler
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        // ---------------------------------
        // Power BI / Fabric Identifiers
        // ---------------------------------
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public Guid DatasetId { get; set; }

        // ---------------------------------
        // Scheduler Display Info
        // ---------------------------------
        [Required]
        [StringLength(200)]
        public string SchedulerName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // ---------------------------------
        // Refresh Options
        // ---------------------------------
        [Required]
        public bool IsIncrementalRefresh { get; set; } = false;

        [StringLength(2000)]
        public string? RefreshTables { get; set; }

        // ---------------------------------
        // Schedule Configuration
        // ---------------------------------
        [Required]
        public DateTime ScheduleStartDate { get; set; }

        public DateTime? ScheduleEndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string RepeatType { get; set; } = "Daily";

        [Required]
        public int ScheduleHour { get; set; }    // 1–12

        [Required]
        public int ScheduleMinute { get; set; }  // 0–59

        [Required]
        [StringLength(2)]
        public string ScheduleAMPM { get; set; } = "AM";

        [Required]
        [StringLength(200)]
        public string TimeZone { get; set; } = "UTC";

        // ---------------------------------
        // Status & Execution
        // ---------------------------------
        [Required]
        public bool IsActive { get; set; } = true;

        public DateTime? LastRunAt { get; set; }

        [StringLength(20)]
        public string? LastRunStatus { get; set; }   // Success / Failed

        public string? LastRunMessage { get; set; }

        // ---------------------------------
        // Metadata (UserId-based)
        // ---------------------------------
        [Required]
        public int CreatedBy { get; set; }      // 👈 UserId

        public int? UpdatedBy { get; set; }     // 👈 UserId

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        // ---------------------------------
        // NOT MAPPED – CALCULATED FIELDS
        // ---------------------------------
        [NotMapped]
        public DateTime ScheduledDateTimeLocal
        {
            get
            {
                int hour24 =
                    ScheduleAMPM.ToUpper() == "PM" && ScheduleHour != 12 ? ScheduleHour + 12 :
                    ScheduleAMPM.ToUpper() == "AM" && ScheduleHour == 12 ? 0 :
                    ScheduleHour;

                return new DateTime(
                    ScheduleStartDate.Year,
                    ScheduleStartDate.Month,
                    ScheduleStartDate.Day,
                    hour24,
                    ScheduleMinute,
                    0
                );
            }
        }

        [NotMapped]
        public bool IsDue
        {
            get
            {
                if (!IsActive) return false;

                var now = DateTime.UtcNow;

                if (ScheduleEndDate != null && now.Date > ScheduleEndDate.Value.Date)
                    return false;

                return now.Hour == ScheduledDateTimeLocal.Hour &&
                       now.Minute == ScheduledDateTimeLocal.Minute;
            }
        }
    }
}
