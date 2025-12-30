using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class UpdateBookmarkDto
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string BookmarkState { get; set; }
        public string BookmarkName { get; set; }
        public string PageName { get; set; }
        public string? PageNumber { get; set; }
    }

}
