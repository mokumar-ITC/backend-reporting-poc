using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class SubscriptionPlanFullDetailsDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public bool IsActive { get; set; }

        public List<FeatureDto> Features { get; set; } = new();
    }

    public class FeatureDto
    {
        public int FeatureId { get; set; }
        public string FeatureName { get; set; } = "";
    }

}
