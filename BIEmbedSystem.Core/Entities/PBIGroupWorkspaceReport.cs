using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_Group_Workspace_Report")]
    public class PBIGroupWorkspaceReport: BaseClass
    {

        public int Id { get; set; }

        [Column("PBI_Worksapce_Report_Id")]
        public string? PBIWorksapceReportId { get; set; }

        public string? GroupName { get; set; }
    }
}
