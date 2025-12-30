using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("OrganizationSubscriptions")]
    public class OrganizationSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrgSubscriptionId { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public int PlanId { get; set; }
        public SubscriptionPlan Plan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }

        public ICollection<UserSubscription> UserSubscriptions { get; set; }

    }
        
}
