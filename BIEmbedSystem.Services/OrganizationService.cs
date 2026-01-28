using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;



namespace BIEmbedSystem.Services
{
    public class OrganizationService
    {
        private readonly MDMDbContext _db;
        private readonly IWebHostEnvironment _env;
        
        public OrganizationService(MDMDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET ALL
        public async Task<List<OrganizationDto>> GetAllAsync()
        {
            return await _db.Organizations
                .Select(o => new OrganizationDto
                {
                    OrganizationId = o.OrganizationId,
                    Name = o.Name,
                    Domain = o.Domain,
                    CreatedOn = o.CreatedOn,
                    IsActive = o.IsActive
                }).ToListAsync();
        }

        // GET BY ID
        public async Task<OrganizationDto?> GetByIdAsync(int id)
        {
            return await _db.Organizations
                .Where(o => o.OrganizationId == id)
                .Select(o => new OrganizationDto
                {
                    OrganizationId = o.OrganizationId,
                    Name = o.Name,
                    DisplayName = o.DisplayName,
                    Language = o.Language,
                    Domain = o.Domain,

                    EmbeddedCapacityName = string.IsNullOrEmpty(o.EmbeddedCapacityName)
                        ? ""
                        : o.EmbeddedCapacityName,

                    WorkspaceId = string.IsNullOrEmpty(o.WorkspaceId)
                        ? ""
                        : o.WorkspaceId,
                    AuthScheme = o.AuthScheme,
                    CreatedOn = o.CreatedOn,
                    IsActive = o.IsActive,
                    LogoUrl = o.LogoUrl
                })
                .FirstOrDefaultAsync();

        }
        public async Task<OrganizationDto?> GetByNameAsync(string name)
        {
            return await _db.Organizations
                .Where(o => o.Name == name)
                .Select(o => new OrganizationDto
                {
                    OrganizationId = o.OrganizationId,
                    Name = o.Name,
                    DisplayName = o.DisplayName,
                    Language = o.Language,
                    Domain = o.Domain,

                    EmbeddedCapacityName = string.IsNullOrEmpty(o.EmbeddedCapacityName)
                        ? ""
                        : o.EmbeddedCapacityName,

                    WorkspaceId = string.IsNullOrEmpty(o.WorkspaceId)
                        ? ""
                        : o.WorkspaceId,
                    AuthScheme = o.AuthScheme,
                    CreatedOn = o.CreatedOn,
                    IsActive = o.IsActive,
                    LogoUrl = o.LogoUrl
                })
                .FirstOrDefaultAsync();

        }

        private async Task<bool> NameExistsForOtherOrgAsync(string name, int? organizationId)
        {
            return await _db.Organizations
                .AnyAsync(o =>
                    o.Name == name &&
                    (!organizationId.HasValue || o.OrganizationId != organizationId.Value));
        }

        public async Task<OrganizationDto?> CreateOrUpdateAsync(
        CreateOrganizationRequest request,
        int? organizationId = null)
        {
            // 🔒 Name exists for another org → block
            if (await NameExistsForOtherOrgAsync(request.Name, organizationId))
                return null;

            Organization organization;

            if (organizationId.HasValue)
            {
                // 🔄 UPDATE
                organization = await _db.Organizations
                    .FirstOrDefaultAsync(o => o.OrganizationId == organizationId);


                if (organization == null)
                    return null;

                organization.Name = request.Name;
                organization.DisplayName = request.DisplayName;
                organization.Domain = request.DomainUrl;
                organization.AuthScheme = request.AuthenticationScheme;
                organization.Language = request.Language;
                organization.IsActive = request.IsActive;
                organization.EmbeddedCapacityName = request.PowerBI?.CapacityId;
                organization.WorkspaceId = request.PowerBI?.WorkspaceId;
                //organization.Update = DateTime.UtcNow;
            }
            else
            {
                // ➕ CREATE
                organization = new Organization
                {
                    Name = request.Name,
                    DisplayName = request.DisplayName,
                    Domain = request.DomainUrl,
                    AuthScheme = request.AuthenticationScheme,
                    Language = request.Language,
                    IsActive = request.IsActive,
                    ParentGroup = "",
                    AdminGroup="",
                    LogoUrl="",
                    GoogleAnalyticsCode="",
                    EmbeddedCapacityName = request.PowerBI?.CapacityId,
                    WorkspaceId = request.PowerBI?.WorkspaceId,
                    CreatedOn = DateTime.UtcNow
                };

                _db.Organizations.Add(organization);
                await _db.SaveChangesAsync();
                var gotId  = await GetByNameAsync(organization.Name);
                //now make subcriptionfor new organisation singup
                var entity = new OrganizationSubscription
                {
                    OrganizationId = gotId.OrganizationId,
                    PlanId = 1,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(30),
                    IsActive = true
                };

                _db.OrganizationSubscriptions.Add(entity);
            }

            await _db.SaveChangesAsync();
            return await GetByIdAsync(organization.OrganizationId);
        }

        // CREATE
        public async Task<OrganizationDto?> CreateAsync(CreateOrganizationRequest request)
        {
            if (await NameExistsAsync(request.Name))
                return null;  // controller will handle response

            var organization = new Organization
            {
                Name = request.Name,
                DisplayName = request.DisplayName,
                Domain = request.DomainUrl,
                AuthScheme = request.AuthenticationScheme,
                Language = request.Language,
                IsActive = request.IsActive,

                EmbeddedCapacityName = request.PowerBI?.CapacityId,
                WorkspaceId = request.PowerBI?.WorkspaceId,

                CreatedOn = DateTime.UtcNow
            };

            _db.Organizations.Add(organization);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(organization.OrganizationId);
        }


        // UPDATE
        public async Task<OrganizationDto?> UpdateAsync(int id, OrganizationUpdateRequest request)
        {
            var org = await _db.Organizations.FindAsync(id);
            if (org == null) return null;

            org.Name = request.Name;
            org.Domain = request.Domain;
            org.IsActive = request.IsActive;
            org.LogoUrl = request.LogoUrl;
            await _db.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        // DELETE
        public async Task<bool> DeleteAsync(int id)
        {
            var org = await _db.Organizations.FindAsync(id);
            if (org == null) return false;

            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> NameExistsAsync(string name)
        {
            return await _db.Organizations
                .AnyAsync(o => o.Name.ToLower() == name.ToLower());
        }
        public async Task<string?> UploadLogoAsyncv2(int id, IFormFile file)
        {
            var org = await _db.Organizations.FindAsync(id);
            if (org == null) return null;

            var webRoot = _env.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var logosPath = Path.Combine(webRoot, "logos");

            if (!Directory.Exists(logosPath))
                Directory.CreateDirectory(logosPath);

            var safeName = Path.GetFileName(file.FileName);
            var fileName = $"{Guid.NewGuid()}_{safeName}";
            var filePath = Path.Combine(logosPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            org.LogoUrl = $"/logos/{fileName}";
            await _db.SaveChangesAsync();

            return org.LogoUrl;
        }

        public async Task<string?> UploadLogoAsync(int id, IFormFile file)
        {
            // Check org exists
            var org = await _db.Organizations.FindAsync(id);
            if (org == null) return null;

            // Make logos folder
            var rootPath = GetRootPath();
            var folderPath = Path.Combine(rootPath, "logos");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            // Generate file name
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(folderPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var logoUrl = $"/logos/{fileName}";

            
            org.LogoUrl = logoUrl;
            await _db.SaveChangesAsync();

            return logoUrl;
        }

        private string GetRootPath()
        {
            // If ASP.NET environment is available
            if (_env != null && !string.IsNullOrEmpty(_env.WebRootPath))
                return _env.WebRootPath;

            // Fallback when running outside ASP.NET or DI fails
            var fallback = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            if (!Directory.Exists(fallback))
                Directory.CreateDirectory(fallback);

            return fallback;
        }

    }
}
