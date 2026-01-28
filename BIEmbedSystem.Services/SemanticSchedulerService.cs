using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class SemanticSchedulerService : ISemanticSchedulerService
    {
        private readonly MDMDbContext _db;

        public SemanticSchedulerService(MDMDbContext db)
        {
            _db = db;
        }

        public async Task<int> CreateAsync(SemanticSchedulerCreateDto dto, int userId)
        {
            var entity = new SemanticScheduler
            {
                WorkspaceId = dto.WorkspaceId,
                DatasetId = dto.DatasetId,
                SchedulerName = dto.SchedulerName,
                Description = dto.Description,
                IsIncrementalRefresh = dto.IsIncrementalRefresh,
                RefreshTables = dto.RefreshTables,
                ScheduleStartDate = dto.ScheduleStartDate,
                ScheduleEndDate = dto.ScheduleEndDate,
                RepeatType = dto.RepeatType,
                ScheduleHour = dto.ScheduleHour,
                ScheduleMinute = dto.ScheduleMinute,
                ScheduleAMPM = dto.ScheduleAMPM,
                TimeZone = dto.TimeZone,
                CreatedBy = userId
            };

            _db.SemanticSchedulers.Add(entity);
            await _db.SaveChangesAsync();

            return entity.Id!.Value;
        }


        public async Task<bool> UpdateAsync(int id, SemanticSchedulerUpdateDto dto, int userId)
        {
            var entity = await _db.SemanticSchedulers.FindAsync(id);
            if (entity == null) return false;

            entity.SchedulerName = dto.SchedulerName;
            entity.Description = dto.Description;
            entity.IsIncrementalRefresh = dto.IsIncrementalRefresh;
            entity.RefreshTables = dto.RefreshTables;
            entity.ScheduleStartDate = dto.ScheduleStartDate;
            entity.ScheduleEndDate = dto.ScheduleEndDate;
            entity.RepeatType = dto.RepeatType;
            entity.ScheduleHour = dto.ScheduleHour;
            entity.ScheduleMinute = dto.ScheduleMinute;
            entity.ScheduleAMPM = dto.ScheduleAMPM;
            entity.TimeZone = dto.TimeZone;
            entity.UpdatedBy = userId;
            entity.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(int id, bool isActive, int userId)
        {
            var entity = await _db.SemanticSchedulers.FindAsync(id);
            if (entity == null) return false;

            entity.IsActive = isActive;
            entity.UpdatedBy = userId;
            entity.UpdatedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _db.SemanticSchedulers
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync() > 0;
        }

        // ----------------------------------------------------
        // Get by WorkspaceId + DatasetId
        // ----------------------------------------------------
        public async Task<IEnumerable<SemanticScheduler>> GetByWorkspaceAndDatasetAsync(
            Guid workspaceId,
            Guid datasetId
        )
        {
            return await _db.SemanticSchedulers
                .Where(x =>
                    x.WorkspaceId == workspaceId &&
                    x.DatasetId == datasetId
                )
                .OrderByDescending(x => x.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
        // ----------------------------------------------------
        // Get by UserId
        // ----------------------------------------------------
        public async Task<IEnumerable<SemanticScheduler>> GetByUserAsync(int userId)
        {
            return await _db.SemanticSchedulers
                .Where(x => x.CreatedBy == userId)
                .OrderByDescending(x => x.CreatedDate)
                .AsNoTracking()
                .ToListAsync();
        }
        // ----------------------------------------------------
        // ✅ Get by Id
        // ----------------------------------------------------
        public async Task<SemanticScheduler?> GetByIdAsync(int id)
        {
            return await _db.SemanticSchedulers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}
