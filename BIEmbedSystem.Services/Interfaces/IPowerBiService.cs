using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface IPowerBiService
    {
        Task<EmbedResponseDto> GenerateEmbedTokenAsync(EmbedRequestDto request);
    }

}