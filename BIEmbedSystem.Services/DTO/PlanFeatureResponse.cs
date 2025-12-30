using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class PlanFeatureResponse
    {
        public int PlanFeatureId { get; set; }
        public int PlanId { get; set; }
        public int FeatureId { get; set; }
        public string FeatureName { get; set; }
    }
}
