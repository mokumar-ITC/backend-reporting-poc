using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Infrastrucure;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using BIEmbedSystem.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace BIEmbedSystem.Services
{
    public class UserService
    {
        private readonly MDMDbContext _db;
        private readonly EmailService _email;
        private readonly PBIManagementService _pbiMmgtService;
        private readonly IUserSubscriptionService _userSubcriptionSservice;
        private readonly IOrganizationSubscriptionService _organizationSubscriptionService;

        public UserService(MDMDbContext db, EmailService email, PBIManagementService pbiMmgtService, IUserSubscriptionService userSubscriptionService, IOrganizationSubscriptionService organizationSubscription  )
        {
            _db = db;
            _email = email;
            _pbiMmgtService = pbiMmgtService;
            _userSubcriptionSservice = userSubscriptionService;
            _organizationSubscriptionService = organizationSubscription;
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            return await _db.Users
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedOn = u.CreatedOn,
                    IsActive = u.IsActive,
                    OrganizationId = u.OrganizationId
                }).ToListAsync();
        }

        public async Task<List<UserDto>> GetAllByOrgAsync(int id)
        {
            return await _db.Users
                .Where(u => u.OrganizationId == id)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedOn = u.CreatedOn,
                    IsActive = u.IsActive,
                    OrganizationId = u.OrganizationId
                }).ToListAsync();
        }

        public async Task<PagedResponse<UserDto>> GetAllByOrgByPageAsync(
            int organizationId,
            int pageNumber,
            int pageSize,
            string? search
        )
        {
            var query = _db.Users
                .Where(u => u.OrganizationId == organizationId)
                .AsQueryable();

            // 🔍 SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) 
                );
            }

            var totalRecords = await query.CountAsync();

            var users = await query
                .OrderByDescending(u => u.CreatedOn)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedOn = u.CreatedOn,
                    IsActive = u.IsActive,
                    OrganizationId = u.OrganizationId
                })
                .ToListAsync();

            return new PagedResponse<UserDto>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = users
            };
        }


        public async Task<UserDto?> GetByIdAsync(int id)
        {

            return await _db.Users
            .Where(u => u.UserId == id)
            .Join(_db.Roles,
                user => user.Role,
                role => role.Id,
                (user, role) => new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = role.Id,
                    RoleName = role.Name,
                    CreatedOn = user.CreatedOn,
                    IsActive = user.IsActive,
                    OrganizationId = user.OrganizationId
                }
            )
            .FirstOrDefaultAsync();

        }
        public async Task<UserDto?> GetByEmailAsync(string email)
        {

            return await _db.Users
                .Where(u => u.Email == email)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedOn = u.CreatedOn,
                    IsActive = u.IsActive,
                    OrganizationId = u.OrganizationId
                }).FirstOrDefaultAsync();
        }

        public async Task<UserDto?> CreateAsync(UserCreateRequest request)
        {
            try
            {
                // ✅ 1. Validate Organization Exists
                bool orgExists = await _db.Organizations
                    .AnyAsync(o => o.OrganizationId == request.OrganizationId);

                if (!orgExists)
                {
                    throw new Exception($"Organization Name does not exist.");
                }

                // ✅ 2. Hash Password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // ✅ 3. Create User
                var user = new Core.Entities.User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = request.Role,
                    OrganizationId = request.OrganizationId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = hashedPassword
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                var getUserByEmail = await GetByEmailAsync(user.Email);
                var organizationSubscriptions = await _organizationSubscriptionService.GetByOrganizationAsync(request.OrganizationId);
                var getOrganizationSubcription = organizationSubscriptions.FirstOrDefault();
                if (getOrganizationSubcription == null)
                {
                    throw new Exception("Organization subscription not found.");
                }
                //make user Navigation access in PBI
                //var defaultPbiNavConfig = new PBINavigationUserAccess
                //{
                //    UserId = request.FullName,
                //    UserEmail = request.Email,
                //    NagivationId = 0,
                //    ShowDatasetPane = false,
                //    ShowEdit = false,
                //    ShowBookmark = false,
                //    IsActive = false,
                //    OrganizationId = getOrganizationSubcription.OrganizationId
                //};
                //make report access enable
                //await _pbiMmgtService.SavePBINavigationUserAccess(defaultPbiNavConfig, request.Email);
                //make user organisation to be assigned by the organisation
                var defaultUserSubcription = new CreateUserSubscriptionRequest
                {
                    OrgSubscriptionId = getOrganizationSubcription.OrgSubscriptionId,
                    UserId = getUserByEmail.UserId
                };
                await _userSubcriptionSservice.AssignUserAsync(defaultUserSubcription);
                // Return DTO
                return new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedOn = user.CreatedOn,
                    OrganizationId = user.OrganizationId,
                };
            }
            catch (Exception ex)
            {

                // ❗ IMPORTANT: return null to controller, controller will show error
                throw new Exception(ex.Message);
            }
        }

        public async Task<UserDto?> CreateBulkAsync(UserCreateRequest request)
        {
            try
            {
                // ✅ 1. Validate Organization Exists
                bool orgExists = await _db.Organizations
                    .AnyAsync(o => o.OrganizationId == request.OrganizationId);

                if (!orgExists)
                {
                    throw new Exception($"Organization Name does not exist.");
                }

                // ✅ 2. Hash Password
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // ✅ 3. Create User
                var user = new Core.Entities.User
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = request.Role,
                    OrganizationId = request.OrganizationId,
                    CreatedOn = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = hashedPassword
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();
                var getUserByEmail = await GetByEmailAsync(user.Email);
                var organizationSubscriptions = await _organizationSubscriptionService.GetByOrganizationAsync(request.OrganizationId);
                var getOrganizationSubcription = organizationSubscriptions.FirstOrDefault();
                if (getOrganizationSubcription == null)
                {
                    throw new Exception("Organization subscription not found.");
                }
                //make user Navigation access in PBI
                var defaultPbiNavConfig = new PBINavigationUserAccess
                {
                    UserId = request.FullName,
                    UserEmail = request.Email,
                    NagivationId = 0,

                    ShowDatasetPane = false,
                    ShowEdit = false,
                    ShowBookmark = false,

                    // ✅ NEW FLAGS
                    ShareReport = false,
                    ExportReport = false,
                    ScheduleReport = false,
                    ScheduleSemantic = false,

                    IsActive = true,
                    OrganizationId = getOrganizationSubcription.OrganizationId
                };

                //make report access enable
                await _pbiMmgtService.SavePBINavigationUserAccess(defaultPbiNavConfig, request.Email);
                //make user organisation to be assigned by the organisation
                var defaultUserSubcription = new CreateUserSubscriptionRequest
                {
                    OrgSubscriptionId = getOrganizationSubcription.OrgSubscriptionId,
                    UserId = getUserByEmail.UserId
                };
                await _userSubcriptionSservice.AssignUserAsync(defaultUserSubcription);
                // Return DTO
                return new UserDto
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    CreatedOn = user.CreatedOn,
                    OrganizationId = user.OrganizationId,
                };
            }
            catch (Exception ex)
            {
                // ❗ IMPORTANT: return null to controller, controller will show error
                throw new Exception(ex.Message);
            }
        }


        public async Task<UserDto?> UpdateAsync(int id, UserUpdateRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return null;

            user.FullName = request.FullName != "" ? request.FullName : user.FullName;
            user.Email = request.Email != "" ? request.Email: user.Email;
            user.Role = request.Role != 0 ? request.Role : user.Role;
            user.OrganizationId = request.OrganizationId != 0 ? request.OrganizationId : user.OrganizationId;
            user.IsActive = request.IsActive ? true : false;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            }

                await _db.SaveChangesAsync();

            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return false;

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<LoginResult> LoginAsync(string email, string password)
        {
            var access = await _db.NavigationUserAccesses
                .FirstOrDefaultAsync(u => u.UserEmail == email && u.IsActive == true);

            if (access == null)
                return new LoginResult { Success = false, Message = "User have No Access" };

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return new LoginResult { Success = false, Message = "User not found" };

            bool match = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            if (!match)
                return new LoginResult { Success = false, Message = "Invalid password" };

            return new LoginResult
            {
                Success = true,
                User = await GetByIdAsync(user.UserId),
                Message = "Login successful"
            };
        }

        

        public async Task<bool> SendOtpAsync(string email, bool mode)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (!mode && user != null)
            {
                return false;
            }
            if (mode && user == null)
            {
                return false;
            }

            var otp = new Random().Next(100000, 999999).ToString();

            var otpEntry = new PasswordResetOtp
            {
                Email = email,
                Otp = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(10)
            };

            _db.PasswordResetOtps.Add(otpEntry);
            await _db.SaveChangesAsync();

            await _email.SendEmailAsync([email], "Your Password Reset OTP", $"Your OTP is: <b>{otp}</b>");

            return true;
        }
        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var record = await _db.PasswordResetOtps
                .Where(o => o.Email == email && o.Otp == otp && !o.IsUsed)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (record == null || record.ExpiryTime < DateTime.UtcNow)
                return false;

            record.IsUsed = true;
            await _db.SaveChangesAsync();
            return true;
        }
        public async Task<bool> ResetPasswordAsync(string email, string newPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _db.SaveChangesAsync();

            return true;
        }
        public async Task<BulkUserCreateResult> BulkCreateAsync(IFormFile file)
        {
            var result = new BulkUserCreateResult();

            using var stream = new StreamReader(file.OpenReadStream());
            var records = CsvHelperUtility.ReadCsv<UserCreateRequest>(stream);

            result.Total = records.Count;

            foreach (var request in records)
            {
                try
                {
                    // 🔒 Validation
                    if (string.IsNullOrWhiteSpace(request.Email))
                        throw new Exception("Email is required.");

                    if (await _db.Users.AnyAsync(u => u.Email == request.Email))
                        throw new Exception($"User already exists: {request.Email}");

                    // ✅ Reuse existing single create logic
                    await CreateBulkAsync(request);

                    result.Success++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"{request.Email}");
                }
            }

            return result;
        }

        public async Task<bool> BulkUpdateAsync(BulkUserUpdateRequest request)
        {
            if (request.UserIds == null || !request.UserIds.Any())
                throw new Exception("No users selected for bulk update.");

            const int SUPER_ADMIN_ROLE_ID = 1;

            // 🔒 Fetch only users belonging to same organization
            var users = await _db.Users
                .Where(u =>
                    request.UserIds.Contains(u.UserId) &&
                    u.OrganizationId == request.OrganizationId)
                .ToListAsync();

            if (!users.Any())
                throw new Exception("No valid users found.");

            foreach (var user in users)
            {
                // 🔒 Never allow SuperAdmin to be changed
                if (user.Role == SUPER_ADMIN_ROLE_ID)
                    continue;

                if (request.RoleId.HasValue && request.RoleId.Value > 0)
                {
                    // ⛔ Prevent assigning SuperAdmin in bulk
                    if (request.RoleId.Value == SUPER_ADMIN_ROLE_ID)
                        throw new Exception("Cannot assign SuperAdmin role via bulk update.");

                    user.Role = request.RoleId.Value;
                }

                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                }
            }

            await _db.SaveChangesAsync();
            return true;
        }


    }
}
