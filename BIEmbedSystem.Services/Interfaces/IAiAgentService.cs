using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface IAiAgentService
    {
        //Task<string> CreateAgentAsync(CreateAgentDto dto);
        //Task<string> UpdateAgentAsync(UpdateAgentDto dto);

        //Task<CheckAgentResponse> CheckAgentAsync(CheckAgentDto dto);
        //Task DeleteAgentAsync(Guid id);
        // ✅ New
        Task<AiQueryResponseDto> QueryAsync(AiQueryRequestDto dto);
    }
}
