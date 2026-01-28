using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("Users")]

    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        public int OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public string FullName { get; set; }
        public string Email { get; set; }
        public int Role { get; set; }

        public ICollection<UserSubscription> UserSubscriptions { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public string PasswordHash { get; set; } = "";

    }

}
