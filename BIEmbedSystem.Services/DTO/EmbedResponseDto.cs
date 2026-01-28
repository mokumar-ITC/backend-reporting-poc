using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class EmbedResponseDto
    {
        public string EmbedToken { get; set; }
        public DateTime ExpiresOn { get; set; }
        public string EmbedUrl { get; set; }
        public string ReportId { get; set; }
    }
}
