using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("UserSubscriptions")]
    public class UserSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserSubscriptionId { get; set; }

        public int OrgSubscriptionId { get; set; }
        public OrganizationSubscription OrgSubscription { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.UtcNow;
    }

}
