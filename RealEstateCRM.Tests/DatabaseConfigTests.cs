using Microsoft.Extensions.Configuration;
using RealEstateCRM.Infrastructure;

namespace RealEstateCRM.Tests;

public class DatabaseConfigTests
{
    [Fact]
    public void Postgres_url_is_converted_to_npgsql_connection_string()
    {
        var cs = DatabaseConfig.ToNpgsqlConnectionString("postgres://crm_user:p%40ss@db.example.com:5433/crm");

        Assert.Contains("Host=db.example.com", cs);
        Assert.Contains("Port=5433", cs);
        Assert.Contains("Database=crm", cs);
        Assert.Contains("Username=crm_user", cs);
        Assert.Contains("Password=p@ss", cs);
    }

    [Fact]
    public void Missing_port_defaults_to_5432()
    {
        var cs = DatabaseConfig.ToNpgsqlConnectionString("postgresql://u:p@host/db");
        Assert.Contains("Port=5432", cs);
    }

    [Fact]
    public void DATABASE_URL_selects_postgres()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DATABASE_URL"] = "postgres://u:p@host:5432/db" })
            .Build();

        var (provider, _) = DatabaseConfig.Resolve(config);
        Assert.True(DatabaseConfig.IsPostgres(provider));
    }

    [Fact]
    public void Defaults_to_sqlite()
    {
        var config = new ConfigurationBuilder().Build();
        var (provider, cs) = DatabaseConfig.Resolve(config);

        Assert.False(DatabaseConfig.IsPostgres(provider));
        Assert.Contains("Data Source", cs);
    }
}
