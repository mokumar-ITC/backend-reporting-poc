using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.Extensions.Logging;
using BIEmbedSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BIEmbedSystem.Services
{
    public class PlanFeatureService : IPlanFeatureService
    {
        private readonly ILogger<PlanFeatureService> _logger;
        private readonly MDMDbContext _db;

        public PlanFeatureService(MDMDbContext db, ILogger<PlanFeatureService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ============================================================
        // CREATE
        // ============================================================
        public async Task<PlanFeatureResponse> CreateAsync(CreatePlanFeatureRequest request)
        {
            _logger.LogInformation("🚀 Creating PlanFeature [PlanId={PlanId}, FeatureId={FeatureId}]",
                request.PlanId, request.FeatureId);

            try
            {
                bool exists = await _db.PlanFeatures
                    .AnyAsync(x => x.PlanId == request.PlanId && x.FeatureId == request.FeatureId);

                if (exists)
                {
                    _logger.LogWarning("⚠️ Cannot create — feature already assigned [PlanId={PlanId}, FeatureId={FeatureId}]",
                        request.PlanId, request.FeatureId);
                    throw new Exception("Feature already added to this plan.");
                }

                var entity = new PlanFeature
                {
                    PlanId = request.PlanId,
                    FeatureId = request.FeatureId
                };

                _db.PlanFeatures.Add(entity);
                await _db.SaveChangesAsync();

                var feature = await _db.SubscriptionFeatures.FindAsync(request.FeatureId);

                _logger.LogInformation("✅ Created PlanFeature successfully [Id={Id}]", entity.PlanFeatureId);

                return new PlanFeatureResponse
                {
                    PlanFeatureId = entity.PlanFeatureId,
                    PlanId = entity.PlanId,
                    FeatureId = entity.FeatureId,
                    FeatureName = feature?.FeatureName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "❌ Error creating PlanFeature [PlanId={PlanId}, FeatureId={FeatureId}]",
                    request.PlanId, request.FeatureId);
                throw;
            }
        }

        // ============================================================
        // GET FEATURES BY PLAN
        // ============================================================
        public async Task<IEnumerable<PlanFeatureResponse>> GetByPlanAsync(int planId)
        {
            _logger.LogInformation("📥 Fetching PlanFeatures for PlanId={PlanId}", planId);

            try
            {
                var result = await _db.PlanFeatures
                    .Where(pf => pf.PlanId == planId)
                    .Select(pf => new PlanFeatureResponse
                    {
                        PlanFeatureId = pf.PlanFeatureId,
                        PlanId = pf.PlanId,
                        FeatureId = pf.FeatureId,
                        FeatureName = pf.Feature.FeatureName
                    })
                    .ToListAsync();

                _logger.LogInformation("📤 Found {Count} features for PlanId={PlanId}", result.Count, planId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching plan features [PlanId={PlanId}]", planId);
                throw;
            }
        }

        // ============================================================
        // UPDATE
        // ============================================================
        public async Task<PlanFeatureResponse> UpdateAsync(int planFeatureId, UpdatePlanFeatureRequest request)
        {
            _logger.LogInformation("✏️ Updating PlanFeature [Id={PlanFeatureId}, NewFeatureId={FeatureId}]",
                planFeatureId, request.FeatureId);

            try
            {
                var entity = await _db.PlanFeatures.FindAsync(planFeatureId);

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Cannot update — PlanFeature not found [Id={PlanFeatureId}]",
                        planFeatureId);
                    throw new Exception("Plan feature not found.");
                }

                entity.FeatureId = request.FeatureId;
                await _db.SaveChangesAsync();

                var feature = await _db.SubscriptionFeatures.FindAsync(request.FeatureId);

                _logger.LogInformation("✅ Successfully updated PlanFeature [Id={PlanFeatureId}]",
                    planFeatureId);

                return new PlanFeatureResponse
                {
                    PlanFeatureId = entity.PlanFeatureId,
                    PlanId = entity.PlanId,
                    FeatureId = entity.FeatureId,
                    FeatureName = feature?.FeatureName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating PlanFeature [Id={PlanFeatureId}]", planFeatureId);
                throw;
            }
        }

        // ============================================================
        // DELETE
        // ============================================================
        public async Task<bool> DeleteAsync(int planFeatureId)
        {
            _logger.LogInformation("🗑 Deleting PlanFeature [Id={PlanFeatureId}]", planFeatureId);

            try
            {
                var entity = await _db.PlanFeatures.FindAsync(planFeatureId);

                if (entity == null)
                {
                    _logger.LogWarning("⚠️ Cannot delete — PlanFeature not found [Id={PlanFeatureId}]",
                        planFeatureId);
                    return false;
                }

                _db.PlanFeatures.Remove(entity);
                await _db.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully deleted PlanFeature [Id={PlanFeatureId}]",
                    planFeatureId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting PlanFeature [Id={PlanFeatureId}]", planFeatureId);
                throw;
            }
        }

        public async Task<PlanFeaturesResponseDto?> GetPlanFeaturesAsync(int planId)
        {
            // 1️⃣ Get plan
            var plan = await _db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanId == planId);

            if (plan == null)
                return null;

            // 2️⃣ JOIN PlanFeatures → Features to get feature NAMES
            var featureNames = await _db.PlanFeatures
                .Where(pf => pf.PlanId == planId)
                .Join(
                    _db.SubscriptionFeatures,                             // join with Features table
                    pf => pf.FeatureId,                       // foreign key
                    f => f.FeatureId,                         // primary key
                    (pf, f) => f.FeatureName                  // return FeatureName
                )
                .ToListAsync();

            // 3️⃣ Create response
            return new PlanFeaturesResponseDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Features = featureNames
            };
        }

        public async Task<SubscriptionPlanFullDetailsDto?> GetFullPlanDetailsAsync(int planId)
        {
            // 1️⃣ Get subscription plan
            var plan = await _db.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanId == planId);

            if (plan == null)
                return null;

            // 2️⃣ Join PlanFeatures → Features to get feature details
            var features = await _db.PlanFeatures
                .Where(pf => pf.PlanId == planId)
                .Join(
                    _db.SubscriptionFeatures,
                    pf => pf.FeatureId,
                    f => f.FeatureId,
                    (pf, f) => new FeatureDto
                    {
                        FeatureId = f.FeatureId,
                        FeatureName = f.FeatureName
                    }
                )
                .ToListAsync();

            // 3️⃣ Create full response
            return new SubscriptionPlanFullDetailsDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Description = plan.Description,
                PriceMonthly = plan.PriceMonthly,
                PriceYearly = plan.PriceYearly,
                IsActive = plan.IsActive,
                Features = features
            };
        }

    }
}
