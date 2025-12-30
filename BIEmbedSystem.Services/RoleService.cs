using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;
using System.Globalization;

namespace BIEmbedSystem.Services
{
    public class RoleService
    {
        private readonly MDMDbContext _db;

        public RoleService(MDMDbContext db)
        {
            _db = db;
        }

        public async Task<List<RoleDto>> GetAllAsync()
        {
            return await _db.Roles
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    OrganizationId = r.OrganizationId,
                    IsActive = r.IsActive
                })
                .ToListAsync();
        }

        public async Task<List<RoleDto>> GetByOrgAsync(int orgId)
        {
            return await _db.Roles
                .Where(r => r.OrganizationId == orgId)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    OrganizationId = r.OrganizationId,
                    IsActive = r.IsActive
                })
                .ToListAsync();
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            return await _db.Roles
                .Where(r => r.Id == id)
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    OrganizationId = r.OrganizationId,
                    IsActive = r.IsActive
                })
                .FirstOrDefaultAsync();
        }

        public async Task<RoleDto> CreateAsync(RoleCreateRequest request)
        {
            var role = new Role
            {
                Name = request.Name,
                OrganizationId = request.OrganizationId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.CreatedBy
            };

            _db.Roles.Add(role);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(role.Id)!;
        }

        public async Task<RoleDto?> UpdateAsync(int id, RoleUpdateRequest request)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null) return null;

            role.Name = request.Name;
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = request.UpdatedBy;

            await _db.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        // Soft delete
        public async Task<bool> DeleteAsync(int id, int? updatedBy)
        {
            var role = await _db.Roles.FindAsync(id);
            if (role == null) return false;

            role.IsActive = false;
            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = updatedBy;

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<int> BulkUpdateAsync(RoleBulkUpdateRequest request)
        {
            var roles = await _db.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                role.IsActive = request.IsActive;
                role.UpdatedAt = DateTime.UtcNow;
                role.UpdatedBy = request.UpdatedBy;
            }

            await _db.SaveChangesAsync();
            return roles.Count;
        }

        public async Task<BulkRoleUploadResult> BulkUploadAsync(IFormFile file)
        {
            var result = new BulkRoleUploadResult();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim
            });

            var records = csv.GetRecords<RoleCsvRequest>().ToList();
            result.Total = records.Count;

            foreach (var record in records)
            {
                try
                {
                    // 🔒 Validation
                    if (string.IsNullOrWhiteSpace(record.Name))
                        throw new Exception("Role name is required.");

                    // Prevent duplicate role per org
                    bool exists = await _db.Roles.AnyAsync(r =>
                        r.Name == record.Name &&
                        r.OrganizationId == record.OrganizationId);

                    if (exists)
                        throw new Exception($"Role already exists: {record.Name}");

                    var role = new Role
                    {
                        Name = record.Name,
                        OrganizationId = record.OrganizationId,
                        IsActive = record.IsActive,
                        CreatedAt = DateTime.UtcNow
                    };

                    _db.Roles.Add(role);
                    await _db.SaveChangesAsync();

                    result.Success++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"[{record.Name}] {ex.Message}");
                }
            }

            return result;
        }
    }
}
