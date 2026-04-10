using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PBI_Navigation_Manage")]
    public class PBINavigationManagement : BaseClass
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? ParentItem { get; set; }

        public string? Group { get; set; }

        public string? Type { get; set; }
        public string? Icon { get; set; }
        public string? Description { get; set; }

        public int? Order { get; set; }

        public bool IsDynamicBinding { get; set; }

        public string? SourceDatasetId { get; set; }
        public string? TargetDatasetId { get; set; }

        public string? WorkspaceId { get; set; }
        public string? ReportId { get; set; }

        public string? ReportPageNumber { get; set; }
        public string? ReportPageName { get; set; }

        public string? EmbedUrl { get; set; }
        public string? DatasetId { get; set; }

        public bool ShowDatasetHistoryPane { get; set; }
        public bool ShowFilterPane { get; set; }
        public bool ShowContentPane { get; set; }
        public bool ShowTitleDescription { get; set; }

        public bool ReportSharingAllowed { get; set; }
        public bool ReportExportAllowed { get; set; }

        public int? RoleId { get; set; }

        public bool IsActive { get; set; }

        // ✅ New fields for AI integration
        public bool AiEnable { get; set; } = false;   // default = false
        public string? AiAgentId { get; set; }        // NVARCHAR(100)

        // ✅ Stores the full lakehouse config as JSON string
        // e.g. {"lakehouse":"lh_acc_dev_gold","tables":[{"tableName":"sla_dim","columns":["sla_id"]}]}
        [Column(TypeName = "nvarchar(max)")]
        public string? LakehouseConfig { get; set; }
    }
}