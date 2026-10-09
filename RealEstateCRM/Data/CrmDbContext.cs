using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RealEstateCRM.Models;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Data
{
    public class CrmDbContext : DbContext
    {
        private readonly ITenantProvider _tenant;

        public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantProvider tenant) : base(options)
        {
            _tenant = tenant;
        }

        /// <summary>Read by the global query filters; re-evaluated for every query on this context instance.</summary>
        public int CurrentCompanyId => _tenant.CompanyId;

        public DbSet<Contact> Contacts => Set<Contact>();
        public DbSet<Property> Properties => Set<Property>();
        public DbSet<Interaction> Interactions => Set<Interaction>();
        public DbSet<Lead> Leads => Set<Lead>();
        public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
        public DbSet<Brokerage> Brokerages => Set<Brokerage>();
        public DbSet<SiteVisit> SiteVisits => Set<SiteVisit>();
        public DbSet<Company> Companies => Set<Company>();
        public DbSet<User> Users => Set<User>();

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Always persist UTC (required by Npgsql timestamptz; harmless for SQLite).
            configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
            configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            StampTenant();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            StampTenant();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        private void StampTenant()
        {
            foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (CurrentCompanyId == 0)
                        throw new InvalidOperationException("Cannot save tenant data without an authenticated company.");
                    entry.Entity.CompanyId = CurrentCompanyId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // A row can never move between companies.
                    entry.Property(nameof(ITenantEntity.CompanyId)).IsModified = false;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Company>(entity =>
            {
                entity.HasIndex(c => c.Code).IsUnique();
                entity.Property(c => c.Code).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(200);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => new { u.CompanyId, u.Username }).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(100);
                entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(200);
                entity.Property(u => u.Role).IsRequired().HasMaxLength(20);
                entity.HasOne(u => u.Company)
                      .WithMany(c => c.Users)
                      .HasForeignKey(u => u.CompanyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── Tenant isolation: every business table is filtered by the caller's company ──
            modelBuilder.Entity<Contact>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Property>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Interaction>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Lead>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<CrmTask>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<Brokerage>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);
            modelBuilder.Entity<SiteVisit>().HasQueryFilter(e => e.CompanyId == CurrentCompanyId);

            modelBuilder.Entity<Contact>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<Property>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<Interaction>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<Lead>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<CrmTask>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<Brokerage>().HasIndex(e => e.CompanyId);
            modelBuilder.Entity<SiteVisit>().HasIndex(e => e.CompanyId);

            modelBuilder.Entity<Contact>(entity =>
            {
                entity.HasIndex(c => c.Email);
                entity.HasIndex(c => new { c.LastName, c.FirstName });
            });

            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.City);
                entity.HasOne(p => p.Owner)
                      .WithMany()
                      .HasForeignKey(p => p.OwnerId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Interaction>(entity =>
            {
                entity.HasOne(i => i.Contact)
                      .WithMany(c => c.Interactions)
                      .HasForeignKey(i => i.ContactId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.Property)
                      .WithMany(p => p.Interactions)
                      .HasForeignKey(i => i.PropertyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Lead>(entity =>
            {
                entity.HasIndex(l => l.Stage);
                entity.HasOne(l => l.Contact)
                      .WithMany()
                      .HasForeignKey(l => l.ContactId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(l => l.Property)
                      .WithMany()
                      .HasForeignKey(l => l.PropertyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<CrmTask>(entity =>
            {
                entity.HasIndex(t => t.DueDate);
                entity.HasIndex(t => t.Status);
                entity.HasOne(t => t.Contact)
                      .WithMany()
                      .HasForeignKey(t => t.ContactId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(t => t.Property)
                      .WithMany()
                      .HasForeignKey(t => t.PropertyId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(t => t.Lead)
                      .WithMany()
                      .HasForeignKey(t => t.LeadId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Brokerage>(entity =>
            {
                entity.HasIndex(b => b.PaymentStatus);
                entity.HasOne(b => b.Lead)
                      .WithMany()
                      .HasForeignKey(b => b.LeadId)
                      .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(b => b.Property)
                      .WithMany()
                      .HasForeignKey(b => b.PropertyId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SiteVisit>(entity =>
            {
                entity.HasIndex(s => s.ScheduledDate);
                entity.HasIndex(s => s.Status);
                entity.HasOne(s => s.Contact)
                      .WithMany()
                      .HasForeignKey(s => s.ContactId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(s => s.Property)
                      .WithMany()
                      .HasForeignKey(s => s.PropertyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }

    public sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
    {
        public UtcDateTimeConverter() : base(
            v => v.Kind == DateTimeKind.Utc ? v : (v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : DateTime.SpecifyKind(v, DateTimeKind.Utc)),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        { }
    }

    public sealed class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
    {
        public NullableUtcDateTimeConverter() : base(
            v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : (v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc))) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
        { }
    }
}
