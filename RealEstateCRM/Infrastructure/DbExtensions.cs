using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Infrastructure
{
    public static class DbExtensions
    {
        /// <summary>
        /// Throws 400 when the referenced row does not exist for the current tenant.
        /// Because of the tenant query filter this also blocks references to another company's data.
        /// </summary>
        public static async Task EnsureExistsAsync<T>(this CrmDbContext db, int? id, string label) where T : class, ITenantEntity
        {
            if (!id.HasValue) return;

            var exists = await db.Set<T>().AnyAsync(e => EF.Property<int>(e, "Id") == id.Value);
            if (!exists)
                throw new BadRequestException($"{label} {id.Value} was not found.");
        }
    }

    public static class Money
    {
        public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
