using Microsoft.PowerBI.Api.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Core.Entities
{
    [Table("Organizations")]

    public class Organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrganizationId { get; set; }
        public string Name { get; set; }
        public string Domain { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; }

        public string? LogoUrl { get; set; } = "";
        public ICollection<User> Users { get; set; }
        public ICollection<OrganizationSubscription> Subscriptions { get; set; }

    }

}
