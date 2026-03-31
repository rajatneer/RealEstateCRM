using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Models;

namespace RealEstateCRM.Data
{
    public class CrmDbContext : DbContext
    {
        public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

                        public DbSet<Contact> Contacts => Set<Contact>();
                        public DbSet<Property> Properties => Set<Property>();
                        public DbSet<Interaction> Interactions => Set<Interaction>();
                        public DbSet<Lead> Leads => Set<Lead>();
                        public DbSet<CrmTask> CrmTasks => Set<CrmTask>();
                        public DbSet<Brokerage> Brokerages => Set<Brokerage>();
                        public DbSet<SiteVisit> SiteVisits => Set<SiteVisit>();
                        public DbSet<Company> Companies => Set<Company>();
                        public DbSet<User> Users => Set<User>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
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
                        entity.HasOne(u => u.Company)
                                .WithMany(c => c.Users)
                                .HasForeignKey(u => u.CompanyId)
                                .OnDelete(DeleteBehavior.Cascade);
                  });

                  base.OnModelCreating(modelBuilder);

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
}
