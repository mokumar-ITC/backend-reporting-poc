using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class ColumnSchema
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public bool IsHidden { get; set; }
    }

    public class MeasureSchema
    {
        public string Name { get; set; }
        public string Expression { get; set; }
        public string FormatString { get; set; }
    }

    public class TableSchema
    {
        public string TableName { get; set; }
        public List<ColumnSchema> Columns { get; set; }
        public List<MeasureSchema> Measures { get; set; }
    }

    public class RelationshipSchema
    {
        public string FromTable { get; set; }
        public string FromColumn { get; set; }
        public string ToTable { get; set; }
        public string ToColumn { get; set; }
        public string CrossFilteringBehavior { get; set; }
        public bool IsActive { get; set; }
    }
}
