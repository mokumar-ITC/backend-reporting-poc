using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class EmbedRequestDto
    {
        public string WorkspaceId { get; set; }    // group id
        public string ReportId { get; set; }
        public string DatasetId { get; set; }
        public string UserEmail { get; set; }      // e.g. mokumar@itconvergence.com
    }
}
