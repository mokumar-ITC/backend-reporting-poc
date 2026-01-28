using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class BookmarkRequestDto
    {
        public int UserId { get; set; }             // Changed to INT
        public string WorkspaceId { get; set; }
        public string ReportId { get; set; }
        public string PageName { get; set; }
        public string? PageNumber { get; set; }
        public string BookmarkState { get; set; }
        public string BookmarkName { get; set; }
    }

}
