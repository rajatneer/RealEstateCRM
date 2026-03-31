using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using System.Security.Cryptography;
using System.Text;

namespace RealEstateCRM.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<AuthController> _logger;

        public AuthController(CrmDbContext context, ILogger<AuthController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            try
            {
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.Code == dto.CompanyCode);
                if (company == null)
                    return Unauthorized("Invalid company code or credentials.");

                var user = await _context.Users.FirstOrDefaultAsync(u => u.CompanyId == company.Id && u.Username == dto.Username && u.IsActive);
                if (user == null)
                    return Unauthorized("Invalid company code or credentials.");

                var hash = HashPassword(dto.Password);
                if (user.PasswordHash != hash)
                    return Unauthorized("Invalid company code or credentials.");

                // For demo: return basic info (no JWT/session yet)
                return Ok(new { user.Id, user.Username, company.Code, company.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for company {CompanyCode}, user {Username}", dto.CompanyCode, dto.Username);
                return StatusCode(500, "An error occurred during login.");
            }
        }

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }

    public class LoginRequestDto
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
