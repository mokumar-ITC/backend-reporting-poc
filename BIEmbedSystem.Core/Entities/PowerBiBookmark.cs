using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PowerBI_Bookmarks")]
    public class PowerBiBookmark
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]// Changed to INT
        public string WorkspaceId { get; set; }
        [Required]
        public string ReportId { get; set; }

        public string PageName { get; set; }

        public string? PageNumber { get; set; }
        public string BookmarkState { get; set; }
        public string BookmarkName { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
    }

}
