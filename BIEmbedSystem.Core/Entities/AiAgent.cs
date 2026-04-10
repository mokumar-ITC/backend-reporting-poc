using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("AiAgents")]
    public class AiAgent
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string AgentName { get; set; }

        [MaxLength(200)]
        public string FoundryAgentId { get; set; }

        [MaxLength(200)]
        public string FabricAgentId { get; set; }

        [MaxLength(200)]
        public string WorkspaceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // 🔗 Navigation Property (1 → Many)
        public ICollection<AiAgentSemanticModel> SemanticModels { get; set; }
            = new List<AiAgentSemanticModel>();
    }
}