using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("OrganizationWorkspaces")]
    public class OrganizationWorkspace
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(Organization))]
        public int OrganizationId { get; set; }

        public Guid WorkspaceId { get; set; }

        [MaxLength(200)]
        public string? WorkspaceName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;

        public Organization Organization { get; set; }
    }
}
