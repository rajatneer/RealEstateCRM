using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RealEstateCRM.Models;

namespace RealEstateCRM.Security
{
    public class JwtSettings
    {
        public const string Section = "Jwt";

        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = "RealEstateCRM";
        public string Audience { get; set; } = "RealEstateCRM";
        public int ExpiryMinutes { get; set; } = 480;
    }

    public interface ITokenService
    {
        (string Token, DateTime ExpiresAt) Create(User user, Company company);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;

        public TokenService(IOptions<JwtSettings> options)
        {
            _settings = options.Value;
        }

        public (string Token, DateTime ExpiresAt) Create(User user, Company company)
        {
            var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("name", user.Username),
                    new Claim("companyId", company.Id.ToString()),
                    new Claim("role", user.Role)
                }),
                Expires = expires,
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                    SecurityAlgorithms.HmacSha256)
            };

            return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
        }
    }
}
