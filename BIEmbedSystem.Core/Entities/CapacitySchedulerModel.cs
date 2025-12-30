using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("Capacity_Scheduler")]
    public class CapacitySchedulerModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CapacityName { get; set; } = string.Empty;

        [Required]
        public DateTime start_time { get; set; }

        public DateTime? end_time { get; set; }

        // This property is auto-calculated when entity is read (not stored in DB)
        [NotMapped]
        public int duration
        {
            get
            {
                if (end_time.HasValue)
                    return (int)(end_time.Value - start_time).TotalMinutes;
                return 0;
            }
        }

        public DateTime? last_run_time { get; set; }

        [Required]
        [StringLength(10)]
        [RegularExpression("Active|Inactive", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string Status { get; set; } = "Active";

        public DateTime? UpdatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
