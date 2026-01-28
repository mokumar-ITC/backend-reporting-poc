using System;
using System.ComponentModel.DataAnnotations;

namespace BIEmbedSystem.Services.DTO.Requests
{
    public class CapacitySchedulerCreateRequest
    {
        // --- Basic Info ---
        [Required(ErrorMessage = "CapacityName is required.")]
        [StringLength(100, ErrorMessage = "CapacityName cannot exceed 100 characters.")]
        public string CapacityName { get; set; } = string.Empty;

        // --- Timing Info ---
        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime start_time { get; set; }

        // EndTime is optional. If not provided, it will be calculated from DurationHours.
        public DateTime? end_time { get; set; }

        // Optional: Duration in hours, used to auto-calculate EndTime if not given.
        [Range(0.1, 1000, ErrorMessage = "DurationHours must be greater than zero.")]
        public double? DurationHours { get; set; }

        // Last run timestamp (optional).
        public DateTime? last_run_time { get; set; }

        // --- Status ---
        [Required]
        [RegularExpression("Active|Inactive", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string Status { get; set; } = "Active";
    }


}
