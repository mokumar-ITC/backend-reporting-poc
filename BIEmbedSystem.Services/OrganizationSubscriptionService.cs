using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;

public class OrganizationSubscriptionService : IOrganizationSubscriptionService
{
    private readonly MDMDbContext _db;
    private readonly ILogger<OrganizationSubscriptionService> _logger;

    public OrganizationSubscriptionService(MDMDbContext db, ILogger<OrganizationSubscriptionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OrganizationSubscriptionResponse> CreateAsync(CreateOrganizationSubscriptionRequest request)
    {
        _logger.LogInformation("Creating org subscription OrgId={OrgId} PlanId={PlanId}", request.OrganizationId, request.PlanId);

        var entity = new OrganizationSubscription
        {
            OrganizationId = request.OrganizationId,
            PlanId = request.PlanId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true
        };

        _db.OrganizationSubscriptions.Add(entity);
        await _db.SaveChangesAsync();

        var plan = await _db.SubscriptionPlans.FindAsync(request.PlanId);
        var Organization = await _db.Organizations.FindAsync(request.OrganizationId);
        return new OrganizationSubscriptionResponse
        {
            OrgSubscriptionId = entity.OrgSubscriptionId,
            OrganizationName = Organization.Name,
            PlanId = entity.PlanId,
            PlanName = plan?.PlanName,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive
        };
    }

    public async Task<OrganizationSubscriptionResponse> UpdateAsync(
     int orgSubscriptionId,
     UpdateOrganizationSubscriptionRequest request)
    {
        // 1. Fetch subscription
        var entity = await _db.OrganizationSubscriptions
            .FirstOrDefaultAsync(x => x.OrgSubscriptionId == orgSubscriptionId);

        if (entity == null)
            throw new KeyNotFoundException("Organization subscription not found");

        // 2. Update subscription fields
        entity.PlanId = request.PlanId;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.IsActive = request.IsActive;

        // Save the subscription update
        await _db.SaveChangesAsync();

        // ---------------------------------------------
        // 3. UPDATE ALL USERS RELATED TO THIS ORG
        // ---------------------------------------------

        var allUsers = await _db.Users
            .Where(u => u.OrganizationId == entity.OrganizationId)
            .ToListAsync();

        foreach (var user in allUsers)
        {
            user.IsActive = request.IsActive;       // Activate or Deactivate user
        }

        await _db.SaveChangesAsync();

        // ---------------------------------------------
        // 5. Load Plan Informations
        // ---------------------------------------------

        var plan = await _db.SubscriptionPlans.FindAsync(entity.PlanId);

        return new OrganizationSubscriptionResponse
        {
            OrgSubscriptionId = entity.OrgSubscriptionId,
            OrganizationId = entity.OrganizationId,
            PlanId = entity.PlanId,
            PlanName = plan?.PlanName,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive
        };
    }


    public async Task<IEnumerable<OrganizationSubscriptionResponse>> GetByOrganizationAsync(int organizationId)
    {
        return await _db.OrganizationSubscriptions
            .Where(x => x.OrganizationId == organizationId)
            .Include(x => x.Plan)
            .Select(x => new OrganizationSubscriptionResponse
            {
                OrgSubscriptionId = x.OrgSubscriptionId,
                OrganizationId = x.OrganizationId,
                PlanId = x.PlanId,
                PlanName = x.Plan.PlanName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            }).ToListAsync();
    }
    public async Task<IEnumerable<OrganizationSubscriptionResponse>> GetAllSubcription()
    {
        return await _db.OrganizationSubscriptions
            .Include(x => x.Plan)
            .Include(x => x.Organization)
            .Select(x => new OrganizationSubscriptionResponse
            {
                OrgSubscriptionId = x.OrgSubscriptionId,
                OrganizationName = x.Organization.Name,
                OrganizationId = x.Organization.OrganizationId,
                PlanId = x.PlanId,
                PlanName = x.Plan.PlanName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                IsActive = x.IsActive
            }).ToListAsync();
    }

    public async Task<OrganizationSubscriptionResponse> GetByIdAsync(int orgSubscriptionId)
    {
        var x = await _db.OrganizationSubscriptions.Include(s => s.Plan).FirstOrDefaultAsync(s => s.OrgSubscriptionId == orgSubscriptionId);
        if (x == null) return null;
        return new OrganizationSubscriptionResponse
        {
            OrgSubscriptionId = x.OrgSubscriptionId,
            OrganizationId = x.OrganizationId,
            PlanId = x.PlanId,
            PlanName = x.Plan.PlanName,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsActive = x.IsActive
        };
    }

    public async Task<bool> DeleteAsync(int orgSubscriptionId)
    {
        var entity = await _db.OrganizationSubscriptions.FindAsync(orgSubscriptionId);
        if (entity == null) return false;
        _db.OrganizationSubscriptions.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
