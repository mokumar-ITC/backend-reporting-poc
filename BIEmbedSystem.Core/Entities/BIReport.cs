using Microsoft.PowerBI.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    internal class BIReport
    {
    }
    public class PowerBI
    {
        // Workspace Id for which Embed token needs to be generated
        public string WorkspaceId { get; set; }

        // Report Id for which Embed token needs to be generated
        public string ReportId { get; set; }
    }
    public class EmbedReport
    {
        // Id of Power BI report to be embedded
        public Guid ReportId { get; set; }

        // Name of the report
        public string ReportName { get; set; }

        // Embed URL for the Power BI report
        public string EmbedUrl { get; set; }
    }
    public class ReportBI
    {
        // Id of Power BI report to be embedded
        public Guid ReportId { get; set; }

        // Name of the report
        public string ReportName { get; set; }

        // Embed URL for the Power BI report
        public string EmbedUrl { get; set; }

        public string DataSetId { get; set; }
    }
    public class EmbedParams
    {
        public List<EmbedReport> EmbedReport { get; set; }
        public string Type { get; set; }
        public EmbedToken EmbedToken { get; set; }
        public string DatasetId { get; set; }
        public string ReportName { get; set; }
        public string ReportDiscription { get; set; }
        public string AssetId { get; set; }

        // NEW:
        public bool IsRlsEnabled { get; set; }   // whether this report/dataset has RLS turned on (source of truth: DB or dataset metadata)
        public bool IsUserAllowed { get; set; }  // whether the requesting user is allowed for this report (based on your PowerBI_Security table)
    }
}
