using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("PlanFeatures")]
    public class PlanFeature
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlanFeatureId { get; set; }

        [Required]
        public int PlanId { get; set; }

        public SubscriptionPlan Plan { get; set; }

        [Required]
        public int FeatureId { get; set; }
        public SubscriptionFeature Feature { get; set; }
    }

}
