using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("UserTracking")]

    public class UserTracking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TrackingId { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int OrganizationId { get; set; }
        public Organization Org { get; set; }
        public string WorkspaceId { get; set; } = "";
        public string ReportId { get; set; } = "";

        public string ActionName { get; set; } = "";
        public string ActionDescription { get; set; } = "";

        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
    }

}
