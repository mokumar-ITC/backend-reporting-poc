using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.EntityFrameworkCore;
using System;

public class SubscriptionPlanService
{
    private readonly MDMDbContext _db;

    public SubscriptionPlanService(MDMDbContext db)
    {
        _db = db;
    }

    // GET ALL
    public async Task<List<SubscriptionPlanDto>> GetAllAsync()
    {
        return await _db.SubscriptionPlans
            .Select(p => new SubscriptionPlanDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                IsActive = p.IsActive
            }).ToListAsync();
    }

    // GET BY ID
    public async Task<SubscriptionPlanDto?> GetByIdAsync(int id)
    {
        return await _db.SubscriptionPlans
            .Where(p => p.PlanId == id)
            .Select(p => new SubscriptionPlanDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                Description = p.Description,
                PriceMonthly = p.PriceMonthly,
                PriceYearly = p.PriceYearly,
                IsActive = p.IsActive
            }).FirstOrDefaultAsync();
    }

    // CREATE
    public async Task<SubscriptionPlanDto> CreateAsync(SubscriptionPlanCreateRequest request)
    {
        var plan = new SubscriptionPlan
        {
            PlanName = request.PlanName,
            Description = request.Description,
            PriceMonthly = request.PriceMonthly,
            PriceYearly = request.PriceYearly,
            IsActive = true
        };

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(plan.PlanId);
    }

    // UPDATE
    public async Task<SubscriptionPlanDto?> UpdateAsync(int id, SubscriptionPlanUpdateRequest request)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return null;

        plan.PlanName = request.PlanName;
        plan.Description = request.Description;
        plan.PriceMonthly = request.PriceMonthly;
        plan.PriceYearly = request.PriceYearly;
        plan.IsActive = request.IsActive;

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    // DELETE
    public async Task<bool> DeleteAsync(int id)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(id);
        if (plan == null) return false;

        _db.SubscriptionPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<SubscriptionPlanFullDetailsDto>> GetAllActivePlansWithDetailsAsync()
    {
        // 1️⃣ Load all active plans
        var plans = await _db.SubscriptionPlans
            .Where(p => p.IsActive)
            .ToListAsync();

        if (!plans.Any())
            return new List<SubscriptionPlanFullDetailsDto>();

        List<SubscriptionPlanFullDetailsDto> response = new();

        foreach (var plan in plans)
        {
            // 2️⃣ Load features for each plan
            var features = await _db.PlanFeatures
                .Where(pf => pf.PlanId == plan.PlanId)
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

            // 3️⃣ Add to response list
            response.Add(new SubscriptionPlanFullDetailsDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Description = plan.Description,
                PriceMonthly = plan.PriceMonthly,
                PriceYearly = plan.PriceYearly,
                IsActive = plan.IsActive,
                Features = features
            });
        }

        return response;
    }
    public async Task<List<SubscriptionPlanFullDetailsDto>> GetAllPlansWithDetailsAsync()
    {
        // 1️⃣ Load all active plans
        var plans = await _db.SubscriptionPlans
            .ToListAsync();

        if (!plans.Any())
            return new List<SubscriptionPlanFullDetailsDto>();

        List<SubscriptionPlanFullDetailsDto> response = new();

        foreach (var plan in plans)
        {
            // 2️⃣ Load features for each plan
            var features = await _db.PlanFeatures
                .Where(pf => pf.PlanId == plan.PlanId)
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

            // 3️⃣ Add to response list
            response.Add(new SubscriptionPlanFullDetailsDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Description = plan.Description,
                PriceMonthly = plan.PriceMonthly,
                PriceYearly = plan.PriceYearly,
                IsActive = plan.IsActive,
                Features = features
            });
        }

        return response;
    }
}
