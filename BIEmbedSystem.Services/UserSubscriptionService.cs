using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly MDMDbContext _db;
        private readonly ILogger<UserSubscriptionService> _logger;

        public UserSubscriptionService(MDMDbContext db, ILogger<UserSubscriptionService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<bool> AssignUserAsync(CreateUserSubscriptionRequest request)
        {
            // Prevent duplicates
            var exists = await _db.UserSubscriptions.AnyAsync(x => x.OrgSubscriptionId == request.OrgSubscriptionId && x.UserId == request.UserId);
            if (exists) { _logger.LogWarning("User already assigned"); return false; }

            var entity = new UserSubscription { OrgSubscriptionId = request.OrgSubscriptionId, UserId = request.UserId };
            _db.UserSubscriptions.Add(entity);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Assigned user {UserId} to orgSubscription {OrgSubId}", request.UserId, request.OrgSubscriptionId);
            return true;
        }

        public async Task<bool> UnassignUserAsync(int userSubscriptionId)
        {
            var e = await _db.UserSubscriptions.FindAsync(userSubscriptionId);
            if (e == null) return false;
            _db.UserSubscriptions.Remove(e);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetUsersInOrgSubscriptionAsync(int orgSubscriptionId)
        {
            return await _db.UserSubscriptions
                .Where(x => x.OrgSubscriptionId == orgSubscriptionId)
                .Include(x => x.User)
                .Select(x => new UserDto { UserId = x.UserId, FullName = x.User.FullName, Email = x.User.Email, IsActive = x.User.IsActive })
                .ToListAsync();
        }
    }

}
