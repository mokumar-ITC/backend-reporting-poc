using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class CreatePlanFeatureRequest
    {
        public int PlanId { get; set; }
        public int FeatureId { get; set; }
    }

}
