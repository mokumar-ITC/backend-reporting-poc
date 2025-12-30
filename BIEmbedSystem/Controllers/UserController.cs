using Asp.Versioning;
using BIEmbedSystem.Core.Entities;
using BIEmbedSystem.Services;
using BIEmbedSystem.Services.DTO;
using BIEmbedSystem.Services.DTO.Requests;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;
        private readonly ILogger<UserController> _logger;

        public UserController(UserService service, ILogger<UserController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: api/v1.0/users
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public async Task<ActionResult> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("org/{id:int}")]
        [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
        public async Task<ActionResult> GetAllByOrgId(int id)
        {
            var users = await _service.GetAllByOrgAsync(id);
            return Ok(users);
        }

        // GET: api/v1.0/users/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> GetById(int id)
        {
            var user = await _service.GetByIdAsync(id);
            if (user == null) return NotFound($"User with Id {id} not found.");

            return Ok(user);
        }

        // POST: api/v1.0/users
        [HttpPost]
        [ProducesResponseType(typeof(UserDto), 201)]
        public async Task<ActionResult> Create([FromBody] UserCreateRequest request)
        {
            try
            {
                var result = await _service.CreateAsync(request);
                return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        // PUT: api/v1.0/users/{id}
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Update(int id, [FromBody] UserUpdateRequest request)
        {
            var updated = await _service.UpdateAsync(id, request);

            if (updated == null) return NotFound($"User with Id {id} not found.");

            return Ok(updated);
        }

        // DELETE: api/v1.0/users/{id}
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);

            if (!deleted) return NotFound($"User with Id {id} not found.");

            return NoContent();
        }

        // POST: api/v1.0/users/login
        [HttpPost("login")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> Login(Services.DTO.Requests.LoginRequest request)
        {
            var result = await _service.LoginAsync(request.Email, request.Password);

            if (result == null || !result.Success)
                return Unauthorized(result?.Message ?? "Invalid email or password");

            return Ok(result.User);
        }

        // POST: api/v1.0/users/login
        [HttpPost("getByEmail")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(401)]
        public async Task<ActionResult> GetByEmail(GetByEmailRequest request )
        {
            var result = await _service.GetByEmailAsync(request.Email);

            return Ok(result);
        }


        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ForgotPassword([FromBody] SendOtpRequest request)
        {
            var sent = await _service.SendOtpAsync(request.Email);

            if (!sent) return NotFound("Email not found.");

            return Ok("OTP sent to your email.");
        }

        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var valid = await _service.VerifyOtpAsync(request.Email, request.Otp);

            if (!valid) return BadRequest("Invalid or expired OTP.");

            return Ok("OTP verified successfully.");
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(UserDto), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult> ResetPassword([FromBody] Services.DTO.ResetPasswordRequest request)
        {
            var updated = await _service.ResetPasswordAsync(request.Email, request.NewPassword);

            if (!updated) return NotFound("User not found.");

            return Ok("Password reset successfully.");
        }

        [HttpPost("bulk")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(BulkRoleUploadResult), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkUpload(
        [FromForm] RoleBulkUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("CSV file is required.");

            var result = await _service.BulkCreateAsync(request.File);
            return Created("", result);
        }

        [HttpPut("bulk")]
        public async Task<IActionResult> BulkUpdateUsers(
        [FromBody] BulkUserUpdateRequest request)
            {
                try
                {
                    await _service.BulkUpdateAsync(request);
                    return Ok(new { message = "Bulk update successful" });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

    }
}
