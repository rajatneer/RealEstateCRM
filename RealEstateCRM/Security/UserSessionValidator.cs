using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RealEstateCRM.Data;

namespace RealEstateCRM.Security
{
    /// <summary>
    /// Checks on every request that the token's user still exists, is active and has not been signed out
    /// (TokenVersion). Results are cached for 30 seconds so this costs ~1 query per user per 30s.
    /// </summary>
    public interface IUserSessionValidator
    {
        Task<bool> IsValidAsync(int userId, int companyId, int tokenVersion);
        void Invalidate(int userId);
    }

    public class UserSessionValidator : IUserSessionValidator
    {
        private sealed record Snapshot(bool Active, int CompanyId, int TokenVersion);

        private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly IMemoryCache _cache;

        public UserSessionValidator(IServiceScopeFactory scopes, IMemoryCache cache)
        {
            _scopes = scopes;
            _cache = cache;
        }

        private static string Key(int userId) => $"session:{userId}";

        public async Task<bool> IsValidAsync(int userId, int companyId, int tokenVersion)
        {
            var snapshot = await _cache.GetOrCreateAsync(Key(userId), async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTime;
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
                return await db.Users.AsNoTracking()
                    .Where(u => u.Id == userId)
                    .Select(u => new Snapshot(u.IsActive, u.CompanyId, u.TokenVersion))
                    .FirstOrDefaultAsync();
            });

            return snapshot is { Active: true } && snapshot.CompanyId == companyId && snapshot.TokenVersion == tokenVersion;
        }

        public void Invalidate(int userId) => _cache.Remove(Key(userId));
    }
}
