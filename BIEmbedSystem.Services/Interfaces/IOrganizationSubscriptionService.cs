using BIEmbedSystem.Services.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services.Interfaces
{
    public interface IOrganizationSubscriptionService
    {
        Task<OrganizationSubscriptionResponse> CreateAsync(CreateOrganizationSubscriptionRequest request);
        Task<IEnumerable<OrganizationSubscriptionResponse>> GetAllSubcription();
        Task<OrganizationSubscriptionResponse> UpdateAsync(int orgSubscriptionId, UpdateOrganizationSubscriptionRequest request);
        Task<IEnumerable<OrganizationSubscriptionResponse>> GetByOrganizationAsync(int organizationId);
        Task<OrganizationSubscriptionResponse> GetByIdAsync(int orgSubscriptionId);
        Task<bool> DeleteAsync(int orgSubscriptionId);
    }

}
