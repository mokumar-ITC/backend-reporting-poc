using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("ReportSubscriptions")]
    public class ReportSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        // Power BI Identifiers
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public Guid ReportId { get; set; }

        // Subscription Display Name
        [Required]
        [StringLength(200)]
        public string SubscriptionName { get; set; } = string.Empty;

        // List of recipients (stored as comma-separated)
        [Required]
        public string Recipients { get; set; } = string.Empty;

        // Attach full report (PDF)
        [Required]
        public bool AttachFullReport { get; set; }

        // ----------------------------
        // NEW VARIABLE ADDED HERE
        // ----------------------------
        /// <summary>
        /// A direct link to share the report subscription details.
        /// </summary>
        [StringLength(2000)]
        public string? ShareLink { get; set; }
        // ----------------------------

        // Schedule
        [Required]
        public DateTime ScheduleStartDate { get; set; }

        public DateTime? ScheduleEndDate { get; set; }

        // Daily, Weekly, Monthly
        [Required]
        [StringLength(20)]
        public string RepeatType { get; set; } = "Daily";

        // Scheduled Time
        [Required]
        public int ScheduleHour { get; set; }    // 1-12

        [Required]
        public int ScheduleMinute { get; set; }  // 00-59

        [Required]
        [StringLength(2)]
        public string ScheduleAMPM { get; set; } = "AM";

        // Timezone
        [Required]
        [StringLength(200)]
        public string TimeZone { get; set; } = "UTC";

        // Status Active/Inactive
        [Required]
        [StringLength(10)]
        [RegularExpression("Active|Inactive", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public bool IsActive { get; set; } = true;

        // Metadata
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }

        public DateTime? LastRunAt { get; set; }


        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // ----------------------------
        // NOT MAPPED CALCULATED FIELDS
        // ----------------------------
        [NotMapped]
        public DateTime ScheduledDateTimeLocal
        {
            get
            {
                int hour24 = ScheduleAMPM.ToUpper() == "PM" && ScheduleHour != 12 ? ScheduleHour + 12 :
                             ScheduleAMPM.ToUpper() == "AM" && ScheduleHour == 12 ? 0 : ScheduleHour;

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
        public bool IsDue // this is optional helper
        {
            get
            {
                if (IsActive != true) return false;
                var now = DateTime.UtcNow;

                // End date validation
                if (ScheduleEndDate != null && now.Date > ScheduleEndDate.Value.Date)
                    return false;

                return now.Hour == ScheduledDateTimeLocal.Hour &&
                       now.Minute == ScheduledDateTimeLocal.Minute;
            }
        }
    }
}