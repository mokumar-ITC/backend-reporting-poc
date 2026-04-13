using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.DTO
{
    public class AiFoundryRequestDto
    {
        public string UserMessage { get; set; } = string.Empty;
        public string? ConversationId { get; set; } // optional for multi-turn
    }

    public class AiFoundryResponseDto
    {
        public string Output { get; set; } = string.Empty;
        public string? ConversationId { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class CreateAgentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Instructions { get; set; }
    }

    public class UpdateAgentDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Instructions { get; set; }
    }

    public class CheckAgentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Version { get; set; }
    }
}
