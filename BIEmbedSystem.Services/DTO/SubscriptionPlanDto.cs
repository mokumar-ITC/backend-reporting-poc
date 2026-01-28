using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class SubscriptionPlanDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal PriceMonthly { get; set; }
        public decimal PriceYearly { get; set; }
        public bool IsActive { get; set; }
    }

}
