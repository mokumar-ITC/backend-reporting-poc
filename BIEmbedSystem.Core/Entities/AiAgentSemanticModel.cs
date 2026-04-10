using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BIEmbedSystem.Core.Entities
{
    [Table("AiAgentSemanticModels")]
    public class AiAgentSemanticModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid AgentId { get; set; }

        [ForeignKey("AgentId")]
        public AiAgent AiAgent { get; set; }

        [Required]
        [MaxLength(200)]
        public string SemanticModelId { get; set; }
    }
}