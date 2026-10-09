using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Tenancy;

namespace RealEstateCRM.Tests;

public class TenantIsolationTests
{
    private sealed class FixedTenant : ITenantProvider
    {
        public FixedTenant(int companyId) => CompanyId = companyId;
        public int CompanyId { get; }
        public int? UserId => null;
        public string? Role => null;
    }

    private static (SqliteConnection Connection, DbContextOptions<CrmDbContext> Options) CreateDatabase()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<CrmDbContext>().UseSqlite(connection).Options;
        using (var setup = new CrmDbContext(options, new FixedTenant(1)))
            setup.Database.EnsureCreated();
        return (connection, options);
    }

    private static Contact NewContact(string first) =>
        new() { FirstName = first, LastName = "Test", Type = ContactType.Buyer };

    [Fact]
    public void Each_company_only_sees_its_own_rows()
    {
        var (connection, options) = CreateDatabase();
        using var _ = connection;

        using (var companyA = new CrmDbContext(options, new FixedTenant(1)))
        {
            companyA.Contacts.Add(NewContact("Alice"));
            companyA.SaveChanges();
        }

        using (var companyB = new CrmDbContext(options, new FixedTenant(2)))
        {
            Assert.Empty(companyB.Contacts.ToList());
            companyB.Contacts.Add(NewContact("Bob"));
            companyB.SaveChanges();
        }

        using (var companyA = new CrmDbContext(options, new FixedTenant(1)))
        {
            var contact = Assert.Single(companyA.Contacts.ToList());
            Assert.Equal("Alice", contact.FirstName);
        }
    }

    [Fact]
    public void CompanyId_is_stamped_on_insert()
    {
        var (connection, options) = CreateDatabase();
        using var _ = connection;

        using var db = new CrmDbContext(options, new FixedTenant(7));
        var contact = NewContact("Carol");
        db.Contacts.Add(contact);
        db.SaveChanges();

        Assert.Equal(7, contact.CompanyId);
    }

    [Fact]
    public void Saving_tenant_data_without_a_company_is_rejected()
    {
        var (connection, options) = CreateDatabase();
        using var _ = connection;

        using var db = new CrmDbContext(options, new FixedTenant(0));
        db.Contacts.Add(NewContact("Nobody"));

        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
    }

    [Fact]
    public void Anonymous_context_sees_nothing()
    {
        var (connection, options) = CreateDatabase();
        using var _ = connection;

        using (var company = new CrmDbContext(options, new FixedTenant(1)))
        {
            company.Contacts.Add(NewContact("Dave"));
            company.SaveChanges();
        }

        using var anonymous = new CrmDbContext(options, new FixedTenant(0));
        Assert.Empty(anonymous.Contacts.ToList());
    }
}
