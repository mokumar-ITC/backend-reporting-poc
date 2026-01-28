using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("Organizations")]
    public class Organization
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrganizationId { get; set; }

        // BASIC INFO
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(200)]
        public string? DisplayName { get; set; }

        [MaxLength(200)]
        public string? Domain { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        // AUTH / SECURITY
        [MaxLength(50)]
        public string? AuthScheme { get; set; }

        [MaxLength(200)]
        public string? ParentGroup { get; set; }

        [MaxLength(200)]
        public string? AdminGroup { get; set; }

        // POWER BI
        [MaxLength(200)]
        public string? EmbeddedCapacityName { get; set; }

        [MaxLength(200)]
        public string? WorkspaceId { get; set; }

        // ANALYTICS
        [MaxLength(50)]
        public string? GoogleAnalyticsCode { get; set; }

        // FEATURE FLAGS
        public bool EnableEmbedLinks { get; set; }
        public bool EnableSPProfiles { get; set; }
        public bool AutoCreateRoles { get; set; }

        // STATUS
        public bool IsActive { get; set; }

        // AUDIT
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        //public DateTime? UpdatedOn { get; set; }

        // BRANDING
        public string? LogoUrl { get; set; }

        // NAVIGATION
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<OrganizationSubscription> Subscriptions { get; set; } = new List<OrganizationSubscription>();
        public ICollection<OrganizationWorkspace> Workspaces { get; set; } = new List<OrganizationWorkspace>();
    }
}
