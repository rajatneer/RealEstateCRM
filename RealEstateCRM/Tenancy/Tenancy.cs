using System.Security.Claims;

namespace RealEstateCRM.Tenancy
{
    /// <summary>Implemented by every business entity that belongs to exactly one company (tenant).</summary>
    public interface ITenantEntity
    {
        int Id { get; }
        int CompanyId { get; set; }
    }

    /// <summary>
    /// Records that belong to one user (an agent). Agents only see their own; Owners see everything in the company.
    /// Set to the creator on insert and changed only through an explicit assignment.
    /// </summary>
    public interface IOwnedEntity
    {
        int? AssignedUserId { get; set; }
    }

    /// <summary>Resolves the current caller's company from the authenticated principal.</summary>
    public interface ITenantProvider
    {
        /// <summary>0 when there is no authenticated tenant (queries then match nothing).</summary>
        int CompanyId { get; }
        int? UserId { get; }
        string? Role { get; }
    }

    public class HttpTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _accessor;

        public HttpTenantProvider(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        private ClaimsPrincipal? User => _accessor.HttpContext?.User;

        public int CompanyId =>
            int.TryParse(User?.FindFirst("companyId")?.Value, out var id) ? id : 0;

        public int? UserId =>
            int.TryParse(User?.FindFirst("sub")?.Value, out var id) ? id : null;

        public string? Role => User?.FindFirst("role")?.Value;
    }
}
