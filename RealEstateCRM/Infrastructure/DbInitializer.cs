using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Security;

namespace RealEstateCRM.Infrastructure
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, IHostEnvironment env)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

            if (db.Database.GetMigrations().Any())
            {
                await db.Database.MigrateAsync();
            }
            else
            {
                logger.LogWarning("No EF migrations found; creating the schema with EnsureCreated. " +
                                  "Generate migrations (see README) before storing real data.");
                await db.Database.EnsureCreatedAsync();
            }

            await BootstrapAsync(db, config, env, logger);
        }

        /// <summary>
        /// Creates the first company and Owner from configuration (Bootstrap:*). Nothing is created in
        /// production unless these values are provided; Development falls back to a documented test login.
        /// </summary>
        private static async Task BootstrapAsync(CrmDbContext db, IConfiguration config, IHostEnvironment env, ILogger logger)
        {
            var code = config["Bootstrap:CompanyCode"];
            var companyName = config["Bootstrap:CompanyName"];
            var username = config["Bootstrap:AdminUsername"];
            var password = config["Bootstrap:AdminPassword"];

            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(username) && string.IsNullOrWhiteSpace(password))
            {
                if (!env.IsDevelopment()) return;

                code = "Test";
                companyName = "Test Company";
                username = "Test";
                password = "Test@12345";
                logger.LogWarning("Development mode: seeding test login Test / Test / Test@12345.");
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogError("Bootstrap requires CompanyCode, AdminUsername and AdminPassword together; skipping.");
                return;
            }

            if (password.Length < 10 && !env.IsDevelopment())
            {
                logger.LogError("Bootstrap:AdminPassword must be at least 10 characters; skipping.");
                return;
            }

            var company = await db.Companies.FirstOrDefaultAsync(c => c.Code == code);
            if (company == null)
            {
                company = new Company { Code = code, Name = string.IsNullOrWhiteSpace(companyName) ? code : companyName };
                db.Companies.Add(company);
                await db.SaveChangesAsync();
            }

            var exists = await db.Users.AnyAsync(u => u.CompanyId == company.Id && u.Username == username);
            if (!exists)
            {
                db.Users.Add(new User
                {
                    CompanyId = company.Id,
                    Username = username,
                    PasswordHash = PasswordHasher.Hash(password),
                    Role = Roles.Owner,
                    IsActive = true
                });
                await db.SaveChangesAsync();
                logger.LogInformation("Bootstrapped owner '{Username}' for company '{Code}'.", username, code);
            }
        }
    }
}
