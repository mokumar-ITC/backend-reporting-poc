using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO.Requests
{
    public class CapacitySchedulerUpdateRequest
    {
        // --- Update capacity status (Active / Inactive) ---
        [RegularExpression("Active|Inactive", ErrorMessage = "Status must be either 'Active' or 'Inactive'.")]
        public string? Status { get; set; }

        // --- Update start time if needed ---
        public DateTime? start_time { get; set; }

        // --- Update end time if manually controlled ---
        public DateTime? end_time { get; set; }

        // --- Optional duration (in hours), auto-calculates EndTime if set ---
        [Range(0.1, 1000, ErrorMessage = "DurationHours must be greater than zero.")]
        public double? duration { get; set; }

        // --- Optionally record the last time it was executed ---
        public DateTime? last_run_time { get; set; }
    }
}
