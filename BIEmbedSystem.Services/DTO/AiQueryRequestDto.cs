using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class AiQueryRequestDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string UserQuery { get; set; } = string.Empty;

        // Optional — frontend can pass lakehouse config directly
        // if not provided, service will look it up from navigation by reportId
        public LakehouseConfigDto? LakehouseConfig { get; set; }
    }

    public class AiQueryResponseDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string UserQuery { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string? SqlGenerated { get; set; }
        public List<Dictionary<string, object?>>? Data { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
