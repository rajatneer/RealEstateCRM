using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Data
{
    /// <summary>
    /// Used only by `dotnet ef` so migrations can be generated without starting the web host
    /// (which would run the startup initializer). Picks the provider the same way the app does.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CrmDbContext>
    {
        public CrmDbContext CreateDbContext(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var (provider, connectionString) = DatabaseConfig.Resolve(config);
            var builder = new DbContextOptionsBuilder<CrmDbContext>();

            if (DatabaseConfig.IsPostgres(provider))
                builder.UseNpgsql(connectionString);
            else
                builder.UseSqlite(connectionString);

            return new CrmDbContext(builder.Options, new DesignTimeTenant());
        }

        private sealed class DesignTimeTenant : ITenantProvider
        {
            public int CompanyId => 0;
            public int? UserId => null;
            public string? Role => null;
        }
    }
}
