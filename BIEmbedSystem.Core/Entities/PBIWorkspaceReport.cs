using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_Workspace_Report_Info")]
    public  class PBIWorkspaceReport:BaseClass
    {
        public int Id { get; set; }

        public string? WorkspaceId { get; set; }

        public string? WorkspaceName { get; set; }

        public string? ReportId { get; set; }

        public string? ReportName { get; set; }

        public string? EmbeddedUrl { get; set; }
    }
}
