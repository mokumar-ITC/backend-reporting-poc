using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.EntityFrameworkCore;

namespace BIEmbedSystem.Services
{
    public class SubscriptionService
    {
        private readonly MDMDbContext _db;
        private readonly ReportPbiEmbedService _powerBiService;

        public SubscriptionService(MDMDbContext db, ReportPbiEmbedService powerBiService)
        {
            _db = db;
            _powerBiService = powerBiService;
        }

        // CREATE
        public async Task<ReportSubscriptionDto> CreateSubscriptionAsync(ReportSubscriptionDto dto)
        {
            var entity = new ReportSubscription
            {
                WorkspaceId = dto.WorkspaceId,
                ReportId = dto.ReportId,
                SubscriptionName = dto.SubscriptionName,
                Recipients = dto.Recipients != null ? string.Join(",", dto.Recipients) : string.Empty,
                AttachFullReport = dto.AttachFullReport,
                ScheduleStartDate = dto.ScheduleStartDate,
                ScheduleEndDate = dto.ScheduleEndDate,
                RepeatType = dto.RepeatType,
                ScheduleHour = dto.ScheduleHour,
                ScheduleMinute = dto.ScheduleMinute,
                ScheduleAMPM = dto.ScheduleAMPM,
                TimeZone = dto.TimeZone,
                IsActive = dto.IsActive, // or true depending on your logic
                CreatedBy = dto.CreatedBy ?? "system",
                CreatedDate = DateTime.UtcNow,
                ShareLink = dto.shareLink
            };

            _db.ReportSubscription.Add(entity);
            await _db.SaveChangesAsync();

            // return the DTO with the generated id and created date
            dto.Id = entity.Id;
            dto.CreatedDate = entity.CreatedDate;
            return dto;
        }

        // UPDATE
        public async Task<bool> UpdateSubscriptionAsync(ReportSubscriptionDto dto)
        {
            var entity = await _db.ReportSubscription.FindAsync(dto.Id);
            if (entity == null) return false;

            entity.SubscriptionName = dto.SubscriptionName;
            entity.Recipients = string.Join(",", dto.Recipients);
            entity.AttachFullReport = dto.AttachFullReport;
            entity.ScheduleStartDate = dto.ScheduleStartDate;
            entity.ScheduleEndDate = dto.ScheduleEndDate;
            entity.RepeatType = dto.RepeatType;
            entity.ScheduleHour = dto.ScheduleHour;
            entity.ScheduleMinute = dto.ScheduleMinute;
            entity.ScheduleAMPM = dto.ScheduleAMPM;
            entity.TimeZone = dto.TimeZone;

            // 🔥 Important Fix
            entity.IsActive = dto.IsActive;

            entity.UpdatedBy = dto.UpdatedBy;
            entity.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }


        // GET BY ID
        public async Task<ReportSubscriptionDto?> GetSubscriptionByIdAsync(int id)
        {
            var entity = await _db.ReportSubscription.FindAsync(id);
            if (entity == null) return null;

            return MapToDto(entity); // safe: mapping happens in memory
        }

        // GET BY UserId
        public async Task<List<ReportSubscriptionDto>> GetSubscriptionByUserIdAsync(int userId)
        {
            var entities = await _db.ReportSubscription.Where(u => u.CreatedBy == userId.ToString()).ToListAsync();
            if (entities == null) return null;

            return entities.Select(MapToDto).ToList(); // safe: mapping happens in memory
        }

        // GET ALL
        public async Task<List<ReportSubscriptionDto>> GetAllSubscriptionsAsync()
        {
            var entities = await _db.ReportSubscription
                                    .AsNoTracking()
                                    .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        // GET by workspace+report
        public async Task<List<ReportSubscriptionDto>> GetSubscriptionsByWorkspaceAndReportAsync(Guid workspaceId, Guid reportId)
        {
            var entities = await _db.ReportSubscription
                                    .AsNoTracking()
                                    .Where(e => e.WorkspaceId == workspaceId && e.ReportId == reportId)
                                    .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        // DELETE
        public async Task<bool> DeleteSubscriptionAsync(int id)
        {
            var entity = await _db.ReportSubscription.FindAsync(id);
            if (entity == null) return false;

            _db.ReportSubscription.Remove(entity);
            await _db.SaveChangesAsync();
            return true;
        }

        private ReportSubscriptionDto MapToDto(ReportSubscription entity)
        {
            // defensively handle null or empty recipients
            var recipients = string.IsNullOrWhiteSpace(entity.Recipients)
                ? new List<string>()
                : entity.Recipients
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();

            return new ReportSubscriptionDto
            {
                Id = entity.Id,
                WorkspaceId = entity.WorkspaceId,
                ReportId = entity.ReportId,
                SubscriptionName = entity.SubscriptionName,
                Recipients = recipients,
                AttachFullReport = entity.AttachFullReport,
                ScheduleStartDate = entity.ScheduleStartDate,
                ScheduleEndDate = entity.ScheduleEndDate,
                RepeatType = entity.RepeatType,
                ScheduleHour = entity.ScheduleHour,
                ScheduleMinute = entity.ScheduleMinute,
                ScheduleAMPM = entity.ScheduleAMPM,
                TimeZone = entity.TimeZone,
                IsActive = entity.IsActive,
                CreatedBy = entity.CreatedBy,
                CreatedDate = entity.CreatedDate,
                UpdatedBy = entity.UpdatedBy,
                UpdatedDate = entity.UpdatedDate
            };
        }
    }
}
