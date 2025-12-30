using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface ISemanticSchedulerService
    {
        Task<int> CreateAsync(SemanticSchedulerCreateDto dto, int userId);
        Task<bool> UpdateAsync(int id, SemanticSchedulerUpdateDto dto, int userId);
        Task<bool> UpdateStatusAsync(int id, bool isActive, int userId);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<SemanticScheduler>> GetByWorkspaceAndDatasetAsync(
            Guid workspaceId,
            Guid datasetId
        );

        Task<IEnumerable<SemanticScheduler>> GetByUserAsync(int userId);

        Task<SemanticScheduler?> GetByIdAsync(int id);

    }
}
