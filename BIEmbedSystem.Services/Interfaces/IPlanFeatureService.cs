using BIEmbedSystem.Services.DTO;
namespace BIEmbedSystem.Services.Interfaces
{
    public interface IPlanFeatureService
    {
        Task<PlanFeatureResponse> CreateAsync(CreatePlanFeatureRequest request);
        Task<IEnumerable<PlanFeatureResponse>> GetByPlanAsync(int planId);
        Task<PlanFeatureResponse> UpdateAsync(int planFeatureId, UpdatePlanFeatureRequest request);
        Task<bool> DeleteAsync(int planFeatureId);
        Task<SubscriptionPlanFullDetailsDto?> GetFullPlanDetailsAsync(int planId);
        Task<PlanFeaturesResponseDto?> GetPlanFeaturesAsync(int planId);
    }

}
