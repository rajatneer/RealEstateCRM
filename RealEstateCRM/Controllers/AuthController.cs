using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Models;
using RealEstateCRM.Security;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private const int MaxFailedLogins = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        // Verified against when the user does not exist, so response time does not reveal valid usernames.
        private static readonly string DummyHash = PasswordHasher.Hash("not-a-real-password");

        private readonly CrmDbContext _context;
        private readonly ITokenService _tokens;
        private readonly ITenantProvider _tenant;
        private readonly ILogger<AuthController> _logger;

        public AuthController(CrmDbContext context, ITokenService tokens, ITenantProvider tenant, ILogger<AuthController> logger)
        {
            _context = context;
            _tokens = tokens;
            _tenant = tenant;
            _logger = logger;
        }

        [AllowAnonymous]
        [EnableRateLimiting("login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Code == dto.CompanyCode);
            var user = company == null
                ? null
                : await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == company.Id && u.Username == dto.Username && u.IsActive);

            if (company == null || user == null)
            {
                PasswordHasher.Verify(dto.Password, DummyHash);
                return InvalidCredentials();
            }

            if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
            {
                _logger.LogWarning("Login blocked for locked-out user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status429TooManyRequests, new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Account temporarily locked",
                    Detail = "Too many failed attempts. Try again in a few minutes."
                });
            }

            var result = PasswordHasher.Verify(dto.Password, user.PasswordHash);
            if (result == PasswordVerifyResult.Failed)
            {
                user.FailedLoginCount++;
                if (user.FailedLoginCount >= MaxFailedLogins)
                {
                    user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                    user.FailedLoginCount = 0;
                    _logger.LogWarning("User {UserId} locked out after repeated failures", user.Id);
                }
                await _context.SaveChangesAsync();
                return InvalidCredentials();
            }

            if (result == PasswordVerifyResult.SuccessRehashNeeded)
                user.PasswordHash = PasswordHasher.Hash(dto.Password);

            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync();

            var (token, expiresAt) = _tokens.Create(user, company);
            return Ok(new
            {
                token,
                expiresAt,
                id = user.Id,
                username = user.Username,
                role = user.Role,
                code = company.Code,
                name = company.Name
            });
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.Id == _tenant.CompanyId);
            if (company == null || _tenant.UserId == null) return Unauthorized();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _tenant.UserId && u.CompanyId == company.Id && u.IsActive);
            if (user == null) return Unauthorized();

            return Ok(new { id = user.Id, username = user.Username, role = user.Role, code = company.Code, name = company.Name });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == _tenant.UserId && u.CompanyId == _tenant.CompanyId && u.IsActive);
            if (user == null) return Unauthorized();

            if (PasswordHasher.Verify(dto.CurrentPassword, user.PasswordHash) == PasswordVerifyResult.Failed)
                throw new BadRequestException("Current password is incorrect.");

            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [Authorize(Roles = Roles.Owner)]
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            if (!Roles.All.Contains(dto.Role))
                throw new BadRequestException($"Role must be one of: {string.Join(", ", Roles.All)}.");

            var companyId = _tenant.CompanyId;
            if (await _context.Users.AnyAsync(u => u.CompanyId == companyId && u.Username == dto.Username))
                throw new ConflictException("That username is already taken.");

            var user = new User
            {
                CompanyId = companyId,
                Username = dto.Username.Trim(),
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = dto.Role,
                IsActive = true
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new { id = user.Id, username = user.Username, role = user.Role });
        }

        private IActionResult InvalidCredentials() =>
            Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = "Invalid company code or credentials."
            });
    }

    public class LoginRequestDto
    {
        [Required, StringLength(100)]
        public string CompanyCode { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(128)]
        public string Password { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required, StringLength(128)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 10)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class CreateUserDto
    {
        [Required, StringLength(100, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(128, MinimumLength = 10)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = Roles.Agent;
    }
}
