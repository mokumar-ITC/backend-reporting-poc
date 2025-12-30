using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.EntityFrameworkCore;

namespace BIEmbedSystem.Services
{
    public class UserTrackingService
    {
        private readonly MDMDbContext _db;

        public UserTrackingService(MDMDbContext db)
        {
            _db = db;
        }

        public async Task<bool> LogActionAsync(UserTrackingRequest request)
        {
            var log = new UserTracking
            {
                UserId = request.UserId,
                WorkspaceId = request.WorkspaceId,
                ReportId = request.ReportId,
                ActionName = request.ActionName,
                ActionDescription = request.ActionDescription,
                OccurredOn = DateTime.UtcNow,
                OrganizationId = request.OrganizationId
            };

            _db.UserTrackings.Add(log);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserTracking>> GetHistoryAsync(int userId)
        {
            return await _db.UserTrackings
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.OccurredOn)
                .ToListAsync();
        }
        public async Task<UserTracking?> GetRefreshLogAysnc(string workspaceId, string reportId, int userId)
        {
            var lastLog =  await _db.UserTrackings
                .Where(x => x.UserId == userId
                         && x.WorkspaceId == workspaceId
                         && x.ReportId == reportId
                         && x.ActionName == "Refresh Report")
                .OrderByDescending(x => x.TrackingId)
                .FirstOrDefaultAsync();
            
            if (lastLog != null)
            {
                var istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

                lastLog.OccurredOn = TimeZoneInfo.ConvertTimeFromUtc(
                    lastLog.OccurredOn,
                    istZone
                );
            }

            return lastLog;

        }


        public async Task<List<UserTrackingResponse>> GetAllHistoryAsync(int OrganizationId)
        {
            return await _db.UserTrackings
                .Where(x => x.OrganizationId == OrganizationId)
                .Include(x => x.User)
                .OrderByDescending(x => x.OccurredOn)
                .Select(x => new UserTrackingResponse
                {
                    Id = x.TrackingId,
                    UserName = x.User.FullName,
                    WorkspaceId = x.WorkspaceId,
                    ReportId = x.ReportId,
                    ActionName = x.ActionName,
                    OccurredOn = x.OccurredOn,
                }).ToListAsync();
        }
    }

}
