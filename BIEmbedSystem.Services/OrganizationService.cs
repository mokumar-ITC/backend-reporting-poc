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
                    Domain = o.Domain,
                    CreatedOn = o.CreatedOn,
                    IsActive = o.IsActive,
                    LogoUrl = o.LogoUrl
                }).FirstOrDefaultAsync();
        }

        // CREATE
        public async Task<OrganizationDto?> CreateAsync(OrganizationCreateRequest request)
        {
            if (await NameExistsAsync(request.Name))
                return null;  // controller will handle response

            var org = new Organization
            {
                Name = request.Name,
                Domain = request.Domain,
                CreatedOn = DateTime.UtcNow,
                IsActive = true
            };

            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(org.OrganizationId);
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
