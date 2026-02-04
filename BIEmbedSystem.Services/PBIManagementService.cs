using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Core.Interfaces;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models;
using Microsoft.PowerBI.Api.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BIEmbedSystem.Services
{
    public class PBIManagementService
    {
        private readonly ILogger<PBIManagementService> _logger;
        private readonly MDMDbContext _db;
        public UserRoleDTO CurrentUserRole { get; set; }
        public string CurrentUserName { get; set; }

        public PBIManagementService(ILogger<PBIManagementService> logger, MDMDbContext db)
        {
            _logger = logger;
            _db = db;
        }
        public async Task<List<PBIGroupWorkspaceReport>> GetGroupWorkspaceReport()
        {
            return await _db.GroupWorkspaceReports.Where(u => u.IsActive == true).ToListAsync();
        }
        public async Task<string> SaveGroupWorkspaceReport(PBIGroupWorkspaceReport groupWorkspaceReport)
        {
            if (groupWorkspaceReport == null)
            {
                _logger.LogError("SaveGroupWorkspaceReport called with null or empty collection.");
                return "WorkspaceReport cannot be null or empty.";
            }
            var exist = await _db.GroupWorkspaceReports.Where(u => u.GroupName.ToUpper() == groupWorkspaceReport.GroupName.ToUpper() 
            && u.PBIWorksapceReportId == groupWorkspaceReport.PBIWorksapceReportId && u.IsActive == true).FirstOrDefaultAsync();
            if (exist == null)
            {
                groupWorkspaceReport.CreatedDate = DateTime.UtcNow;
                groupWorkspaceReport.UpdatedDate = null;
                groupWorkspaceReport.IsActive = true;
                _db.GroupWorkspaceReports.AddAsync(groupWorkspaceReport);
                await _db.SaveChangesAsync(); // Save changes to DB       
                _logger.LogInformation("Saved successfully.");
                return "success";
            }
            
            return "Role already exist";
        }

        public async Task<List<PBIMenubarByGroup>> GetMenubarByGroup()
        {
            return await _db.MenubarByGroups.Where(u => u.IsActive == true).ToListAsync();
        }
        public async Task<string> SaveMenubarByGroup(PBIMenubarByGroup menubarByGroup)
        {
            if (menubarByGroup == null)
            {
                _logger.LogError("SavemenubarByGroup called with null or empty collection.");
                return "menubarByGroup cannot be null or empty.";
            }
            var existRules = await _db.MenubarByGroups.Where(u => u.GroupName.ToUpper() == menubarByGroup.GroupName.ToUpper()
            && u.MenuName.ToUpper() == menubarByGroup.MenuName.ToUpper() && u.IsActive == true).FirstOrDefaultAsync();
            if (existRules == null)
            {
                menubarByGroup.CreatedDate = DateTime.UtcNow;
                menubarByGroup.UpdatedDate = null;
                menubarByGroup.IsActive = true;
                await _db.MenubarByGroups.AddAsync(menubarByGroup);
                await _db.SaveChangesAsync(); // Save changes to DB       
                _logger.LogInformation("Saved successfully.");
                return "success";
            }

            return "Role already exist";
        }

        public async Task<List<PBIWorkspaceReport>> GetWorkspaceReport()
        {
            return await _db.WorkspaceReports.Where(u => u.IsActive == true).ToListAsync();
        }
        public async Task<string> SaveWorkspaceReport(PBIWorkspaceReport workspaceReport)
        {
            if (workspaceReport == null)
            {
                _logger.LogError("SaveWorkspaceReport called with null or empty collection.");
                return "WorkspaceReport cannot be null or empty.";
            }
            var existRules = await _db.WorkspaceReports.Where(u => u.ReportId == workspaceReport.ReportId
            && u.WorkspaceId == workspaceReport.WorkspaceId && u.IsActive == true).FirstOrDefaultAsync();
            if (existRules == null)
            {
                workspaceReport.CreatedDate = DateTime.UtcNow;
                workspaceReport.UpdatedDate = null;
                workspaceReport.IsActive = true;
                _db.WorkspaceReports.Add(workspaceReport);
                await _db.SaveChangesAsync(); // Save changes to DB       
                _logger.LogInformation("Saved successfully.");
                return "success";
            }

            return "Role already exist";
        }

        public async Task<List<PBINavigationManagement>> GetPBINavigationManagement()
        {
            return await _db.NavigationManagements
            .Where(u => u.IsActive == true && u.Id != 43)
            .OrderBy(u => u.Order)   // 👈 sort DESC
            .ToListAsync();
        }
        public async Task<PBINavigationManagement> GetPBINavigationManagementById( int id)
        {
            return await _db.NavigationManagements.Where(u => u.IsActive == true && u.Id==id).FirstOrDefaultAsync();
        }
        public async Task<int> DeletePBINavigationManagementById(int id)
        {
            return await _db.NavigationManagements
                .Where(u => u.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task<List<PBINavigationManagement>> GetUserMenuByGroup(List<int> roleIds)
        {
            // Safety check
            if (roleIds == null || roleIds.Count == 0)
                return new List<PBINavigationManagement>();

            // 🔑 Admin / Super roles
            bool isAdmin = roleIds.Contains(1) || roleIds.Contains(2);

            IQueryable<PBINavigationManagement> query =
                _db.NavigationManagements.Where(u => u.IsActive == true);

            // 🔒 Apply role filter ONLY if not admin
            if (!isAdmin)
            {
                query = query.Where(u => u.RoleId.HasValue && roleIds.Contains(u.RoleId.Value) && u.Id != 43);
            }
            else
            {
                query = query.Where(u => u.Id != 43);
            }
            // ✅ APPLY ORDERING FOR BOTH CASES
            query = query.OrderBy(u => u.Order);

            return await query.ToListAsync();
        }

        public async Task<string> SavePBINavigationManagement(PBINavigationManagement navigationManagement,string userEmail)
        {
            if (navigationManagement == null)
            {
                _logger.LogError("Save Navigation Management called with null or empty collection.");
                return "Navigation Management cannot be null or empty.";
            }
            var existRules = await _db.NavigationManagements.Where(u => u.Id == navigationManagement.Id 
            && u.ParentItem== navigationManagement.ParentItem
            && u.IsActive == true).FirstOrDefaultAsync();
            //check name is already Exist or not
            //var checkName = await _db.NavigationManagements.Where(u => u.Name == navigationManagement.Name).FirstOrDefaultAsync();
            //if (checkName != null)
            //{
            //    return "Name Already exist";
            //}
            if (existRules == null)
            {
                navigationManagement.CreatedDate = DateTime.UtcNow;
                navigationManagement.UpdatedDate = null;
                navigationManagement.CreatedBy = userEmail;
                navigationManagement.IsActive = true;
                navigationManagement.RoleId = navigationManagement.RoleId !=0 ? navigationManagement.RoleId : 5;
                navigationManagement.Type = navigationManagement.Type != "" ? navigationManagement.Type : "Report";
                _db.NavigationManagements.Add(navigationManagement);
                await _db.SaveChangesAsync(); // Save changes to DB       
                _logger.LogInformation("Saved successfully.");
                return "success";
            }
            else
            {
                // Load existing record
                var result = await _db.NavigationManagements
                    .FirstOrDefaultAsync(u => u.Id == navigationManagement.Id);

                if (result == null)
                    return "Record not found";

                result.Name = navigationManagement.Name;
                result.ParentItem = navigationManagement.ParentItem;
                result.Group = navigationManagement.Group;
                result.Description = navigationManagement.Description;
                result.WorkspaceId = navigationManagement.WorkspaceId;
                result.ReportId = navigationManagement?.ReportId;
                result.ReportPageNumber = navigationManagement?.ReportPageNumber;
                result.EmbedUrl = navigationManagement.EmbedUrl;
                result.DatasetId = navigationManagement != null ? navigationManagement.DatasetId :  result.DatasetId;
                result.ShowDatasetHistoryPane = navigationManagement.ShowDatasetHistoryPane;
                result.ShowFilterPane = navigationManagement.ShowFilterPane;
                result.ShowContentPane = navigationManagement.ShowContentPane;
                result.ShowTitleDescription = navigationManagement.ShowTitleDescription;
                result.ReportSharingAllowed = navigationManagement.ReportSharingAllowed;
                result.ReportExportAllowed = navigationManagement.ReportExportAllowed;
                result.RoleId = navigationManagement.RoleId != 0 ? navigationManagement.RoleId : 5;
                result.Type = navigationManagement.Type != "" ? navigationManagement.Type : "Report";
                result.UpdatedDate = DateTime.UtcNow;
                result.UpdatedBy = userEmail;
                result.IsActive = result.IsActive;
                result.CreatedDate = result.CreatedDate;
                result.CreatedBy = result.CreatedBy;
                result.UpdatedDate= DateTime.UtcNow;
                result.UpdatedBy = userEmail;
                _db.NavigationManagements.Update(result);
                await _db.SaveChangesAsync(); // Save changes to DB       
                _logger.LogInformation("Update successfully.");
                return "success";
            }

                
        }

        public async Task<bool> SaveNavigationOrderAsync(
        List<NavigationOrderDto> navigationOrders,
        string userEmail)
        {
            var ids = navigationOrders.Select(x => x.Id).ToList();

            var items = await _db.NavigationManagements
                .Where(n => ids.Contains(n.Id))
                .ToListAsync();

            foreach (var item in items)
            {
                var updatedOrder = navigationOrders
                    .First(x => x.Id == item.Id);

                item.Order = updatedOrder.Order;
                item.UpdatedBy = userEmail;
                item.UpdatedDate = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<PBINavigationUserAccess>> GetPBINavigationUserAccess()
        {
            var result = await _db.NavigationUserAccesses.Where(u => u.IsActive == true).ToListAsync();
            return result;
        }
        public async Task<string> SavePBINavigationUserAccess(PBINavigationUserAccess navigationUserAccess, string userEmail)
        {
            if (navigationUserAccess == null)
            {
                _logger.LogError("Save Navigation Management called with null or empty collection.");
                return "Navigation Management cannot be null or empty.";
            }

            // Get existing active record
            var exist = await _db.NavigationUserAccesses
                .FirstOrDefaultAsync(u =>
                    u.UserEmail == navigationUserAccess.UserEmail &&
                    u.NagivationId == navigationUserAccess.NagivationId);

            // -----------------------------
            // CREATE NEW
            // -----------------------------
            if (exist == null)
            {
                navigationUserAccess.CreatedDate = DateTime.UtcNow;
                navigationUserAccess.CreatedBy = userEmail;
                navigationUserAccess.IsActive = true;
                navigationUserAccess.UpdatedDate = null;

                _db.NavigationUserAccesses.Add(navigationUserAccess);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Saved successfully.");
                return "success";
            }

            // -----------------------------
            // UPDATE EXISTING
            // -----------------------------
            exist.UpdatedDate = DateTime.UtcNow;
            exist.UpdatedBy = userEmail;

            // Update only fields that are allowed to change
            exist.ShowDatasetPane = navigationUserAccess.ShowDatasetPane;
            exist.ShowEdit = navigationUserAccess.ShowEdit;
            exist.ShowBookmark = navigationUserAccess.ShowBookmark;
            exist.ShareReport = navigationUserAccess.ShareReport;
            exist.ScheduleReport = navigationUserAccess.ScheduleReport;
            exist.ExportReport = navigationUserAccess.ExportReport;
            exist.ScheduleSemantic = navigationUserAccess.ScheduleSemantic;


            exist.IsActive = navigationUserAccess.IsActive;

            await _db.SaveChangesAsync();
             
            _logger.LogInformation("Updated successfully.");
            return "success";
        }


        public async Task<List<PBINavigationUserAccess>> GetPBINavigationAccessByUser(string UserEmail)
        {
            var GetAll = await _db.NavigationUserAccesses.Where(u => u.IsActive == true && u.UserEmail==UserEmail).ToListAsync();
            return GetAll;
        }
        
        public async Task<PagedResponse<PBINavigationUserAccess>>GetPBINavigationAccessByOrg(
            int orgId,
            int pageNumber = 1,
            int pageSize = 10,
            string? search = null
            )
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;

            var response = new PagedResponse<PBINavigationUserAccess>
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (orgId <= 0)
                return response;

            var query = _db.NavigationUserAccesses
                .AsNoTracking()
                .Where(x => x.OrganizationId == orgId);

            // 🔍 SEARCH (email / userId)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(x =>
                    x.UserEmail.Contains(search) ||
                    x.UserId.Contains(search)
                );
            }

            response.TotalRecords = await query.CountAsync();
            response.TotalPages = (int)Math.Ceiling(
                response.TotalRecords / (double)pageSize
            );

            response.Data = await query
                .OrderByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return response;
        }

        public async Task<string> BulkUpdatePBINavigationUserAccess(
        BulkNavigationUserAccessUpdateRequest request)
        {
            if (request == null || request.UserIds == null || !request.UserIds.Any())
            {
                _logger.LogError("Bulk update called with empty request.");
                return "No users selected for bulk update.";
            }

            var accesses = await _db.NavigationUserAccesses
                .Where(a =>
                    request.UserIds.Contains(a.Id) &&
                    a.NagivationId == request.NagivationId &&
                    a.OrganizationId == request.OrganizationId)
                .ToListAsync();

            if (!accesses.Any())
                return "No matching navigation access records found.";

            foreach (var access in accesses)
            {
                // Audit
                access.UpdatedDate = DateTime.UtcNow;
                access.UpdatedBy = request.UpdatedBy;

                // 🔒 Apply only provided values
                if (request.ShowDatasetPane.HasValue)
                    access.ShowDatasetPane = request.ShowDatasetPane.Value;

                if (request.ShowEdit.HasValue)
                    access.ShowEdit = request.ShowEdit.Value;

                if (request.ShowBookmark.HasValue)
                    access.ShowBookmark = request.ShowBookmark.Value;

                if (request.ShareReport.HasValue)
                    access.ShareReport = request.ShareReport.Value;

                if (request.ExportReport.HasValue)
                    access.ExportReport = request.ExportReport.Value;

                if (request.ScheduleReport.HasValue)
                    access.ScheduleReport = request.ScheduleReport.Value;

                if (request.ScheduleSemantic.HasValue)
                    access.ScheduleSemantic = request.ScheduleSemantic.Value;

                if (request.IsActive.HasValue)
                    access.IsActive = request.IsActive.Value;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Bulk updated navigation access for {Count} users by {User}",
                accesses.Count,
                request.UpdatedBy
            );

            return "success";
        }

    }
}