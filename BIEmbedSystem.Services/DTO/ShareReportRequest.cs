using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class ShareReportRequest
    {
        public Guid WorkspaceId { get; set; }
        public Guid ReportId { get; set; }
        public List<string> ToEmail { get; set; }
        public string Message { get; set; }
        public string SharedBy { get; set; }
        public string ShareUrl { get; set; }
    }

}
