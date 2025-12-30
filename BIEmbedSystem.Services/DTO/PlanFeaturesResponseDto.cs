using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class PlanFeaturesResponseDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public List<string> Features { get; set; } = new();
    }

}
