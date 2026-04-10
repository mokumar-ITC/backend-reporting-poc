using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class DatasetTablesResponseDto
    {
        public string DatasetId { get; set; } = string.Empty;

        public string DatasetName { get; set; } = string.Empty;

        public string WorkspaceId { get; set; } = string.Empty;

        public string WorkspaceName { get; set; } = string.Empty;

        public string? ReportId { get; set; }

        public string? ReportName { get; set; }

        public string MetadataSource { get; set; } = string.Empty;

        public List<DatasetTableDto> Tables { get; set; } = new();
    }

    public class DatasetTableDto
    {
        public string TableName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public bool IsHidden { get; set; }

        public List<DatasetColumnDto> Columns { get; set; } = new();

        public List<DatasetMeasureDto> Measures { get; set; } = new();
    }
    public class DatasetColumnDto
    {
        public string ColumnName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public bool IsHidden { get; set; }
    }

    public class DatasetMeasureDto
    {
        public string MeasureName { get; set; } = string.Empty;

        public string Expression { get; set; } = string.Empty;

        public bool IsHidden { get; set; }
    }

    public class ReportDatasetTableSummaryDto
    {
        public string ReportId { get; set; } = string.Empty;

        public string ReportName { get; set; } = string.Empty;

        public string DatasetId { get; set; } = string.Empty;

        public string DatasetName { get; set; } = string.Empty;

        public List<string> TableNames { get; set; } = new();

        public string? Error { get; set; }
    }

    public class DatasetTableSummaryDto
    {
        public string DatasetId { get; set; } = string.Empty;

        public string ReportId { get; set; } = string.Empty;

        public string ReportName { get; set; } = string.Empty;

        public string DatasetName { get; set; } = string.Empty;

        public string WorkspaceId { get; set; } = string.Empty;

        public string WorkspaceName { get; set; } = string.Empty;

        public List<string> TableNames { get; set; } = new();

        public int TableCount => TableNames?.Count ?? 0;

        public string? Error { get; set; }
    }
}
