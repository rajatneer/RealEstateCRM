using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using RealEstateCRM.Data;
using RealEstateCRM.Models;
using RealEstateCRM.Security;

namespace RealEstateCRM.Tests;

public sealed class CrmFactory : WebApplicationFactory<Program>
{
    public const string AcmePassword = "Acme-Str0ng-Password!";
    public const string BetaPassword = "Beta-Str0ng-Password!";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"crm_test_{Guid.NewGuid():N}.db");
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public CrmFactory()
    {
        // Environment variables are read by the app's configuration before the host is built.
        Environment.SetEnvironmentVariable("Database__Provider", "Sqlite");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", $"Data Source={_dbPath}");
        Environment.SetEnvironmentVariable("Jwt__Key", "integration-test-signing-key-0123456789abcdef");
        Environment.SetEnvironmentVariable("Bootstrap__CompanyCode", "Acme");
        Environment.SetEnvironmentVariable("Bootstrap__CompanyName", "Acme Realty");
        Environment.SetEnvironmentVariable("Bootstrap__AdminUsername", "owner");
        Environment.SetEnvironmentVariable("Bootstrap__AdminPassword", AcmePassword);
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string company, string username, string password)
    {
        var key = $"{company}/{username}";
        if (!_tokens.TryGetValue(key, out var token))
        {
            var anonymous = CreateClient();
            var response = await anonymous.PostAsJsonAsync("/api/auth/login",
                new { companyCode = company, username, password });
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            token = body.GetProperty("token").GetString()!;
            _tokens[key] = token;
        }

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public void EnsureSecondCompany()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        if (db.Companies.Any(c => c.Code == "Beta")) return;

        var company = new Company { Code = "Beta", Name = "Beta Estates" };
        db.Companies.Add(company);
        db.SaveChanges();
        db.Users.Add(new User
        {
            CompanyId = company.Id,
            Username = "owner",
            PasswordHash = PasswordHasher.Hash(BetaPassword),
            Role = Roles.Owner
        });
        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        SqliteConnection.ClearAllPools();
        try { File.Delete(_dbPath); } catch { /* best effort */ }
    }
}

public class ApiIntegrationTests : IClassFixture<CrmFactory>
{
    private readonly CrmFactory _factory;

    public ApiIntegrationTests(CrmFactory factory) => _factory = factory;

    private static object NewContact(string first = "Ada") =>
        new { firstName = first, lastName = "Lovelace", type = "Buyer" };

    [Fact]
    public async Task Health_check_is_public()
    {
        var response = await _factory.CreateClient().GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/contacts")]
    [InlineData("/api/properties")]
    [InlineData("/api/leads")]
    [InlineData("/api/brokerages")]
    [InlineData("/api/tasks")]
    public async Task Data_endpoints_require_authentication(string url)
    {
        var response = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { companyCode = "Acme", username = "owner", password = "definitely-wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Contact_can_be_created_and_listed_with_total_count_header()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);

        var create = await client.PostAsJsonAsync("/api/contacts", NewContact("Grace"));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetAsync("/api/contacts?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.True(list.Headers.Contains("X-Total-Count"));
        var items = await list.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(items.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Invalid_contact_is_rejected_with_400()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);

        var response = await client.PostAsJsonAsync("/api/contacts",
            new { firstName = "", lastName = "NoFirstName", type = "Buyer" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Another_company_cannot_read_or_reference_my_data()
    {
        _factory.EnsureSecondCompany();
        var acme = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);
        var beta = await _factory.CreateAuthenticatedClientAsync("Beta", "owner", CrmFactory.BetaPassword);

        var created = await acme.PostAsJsonAsync("/api/contacts", NewContact("Private"));
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Direct read -> not found, never leaked.
        Assert.Equal(HttpStatusCode.NotFound, (await beta.GetAsync($"/api/contacts/{id}")).StatusCode);

        // Listing -> Beta sees none of Acme's contacts.
        var betaList = await (await beta.GetAsync("/api/contacts")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, betaList.GetArrayLength());

        // Referencing Acme's contact from a Beta lead -> rejected.
        var lead = await beta.PostAsJsonAsync("/api/leads", new { contactId = id, stage = "New", source = "Website" });
        Assert.Equal(HttpStatusCode.BadRequest, lead.StatusCode);

        // Deleting Acme's contact as Beta -> not found, and it still exists for Acme.
        Assert.Equal(HttpStatusCode.NotFound, (await beta.DeleteAsync($"/api/contacts/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await acme.GetAsync($"/api/contacts/{id}")).StatusCode);
    }

    [Fact]
    public async Task Only_owners_can_create_users()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);

        var created = await owner.PostAsJsonAsync("/api/auth/users",
            new { username = "agent1", password = "Agent-Passw0rd!", role = "Agent" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var agent = await _factory.CreateAuthenticatedClientAsync("Acme", "agent1", "Agent-Passw0rd!");
        var forbidden = await agent.PostAsJsonAsync("/api/auth/users",
            new { username = "agent2", password = "Agent-Passw0rd!", role = "Agent" });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task Responses_do_not_expose_internal_columns()
    {
        var client = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);
        var created = await client.PostAsJsonAsync("/api/contacts", NewContact("Hidden"));
        var raw = await created.Content.ReadAsStringAsync();

        Assert.DoesNotContain("companyId", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("assignedUserId", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Disabling_a_user_invalidates_their_existing_token()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);
        var created = await owner.PostAsJsonAsync("/api/auth/users",
            new { username = "temp", password = "Temp-Passw0rd!!", role = "Agent" });
        var userId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var temp = await _factory.CreateAuthenticatedClientAsync("Acme", "temp", "Temp-Passw0rd!!");
        Assert.Equal(HttpStatusCode.OK, (await temp.GetAsync("/api/contacts")).StatusCode);

        var disable = await owner.PutAsJsonAsync($"/api/auth/users/{userId}/active", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, disable.StatusCode);

        // Same token, now rejected immediately.
        Assert.Equal(HttpStatusCode.Unauthorized, (await temp.GetAsync("/api/contacts")).StatusCode);
    }

    [Fact]
    public async Task Owner_can_assign_a_lead_to_an_agent_who_could_not_see_it_before()
    {
        var owner = await _factory.CreateAuthenticatedClientAsync("Acme", "owner", CrmFactory.AcmePassword);

        var agentCreated = await owner.PostAsJsonAsync("/api/auth/users",
            new { username = "agent-assign", password = "Agent-Passw0rd!", role = "Agent" });
        var agentId = (await agentCreated.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var agent = await _factory.CreateAuthenticatedClientAsync("Acme", "agent-assign", "Agent-Passw0rd!");

        var contact = await owner.PostAsJsonAsync("/api/contacts", NewContact("LeadOwner"));
        var contactId = (await contact.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var lead = await owner.PostAsJsonAsync("/api/leads", new { contactId, stage = "New", source = "Website" });
        Assert.Equal(HttpStatusCode.Created, lead.StatusCode);
        var leadId = (await lead.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        Assert.Equal(HttpStatusCode.NotFound, (await agent.GetAsync($"/api/leads/{leadId}")).StatusCode);

        // Agents cannot assign; owners can.
        Assert.Equal(HttpStatusCode.Forbidden,
            (await agent.PutAsJsonAsync($"/api/leads/{leadId}/assign", new { userId = agentId })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await owner.PutAsJsonAsync($"/api/leads/{leadId}/assign", new { userId = agentId })).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await agent.GetAsync($"/api/leads/{leadId}")).StatusCode);
    }

    [Fact]
    public async Task Security_headers_include_a_content_security_policy()
    {
        var response = await _factory.CreateClient().GetAsync("/healthz");

        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        var csp = string.Join(";", values!);
        Assert.Contains("script-src 'self'", csp);
        Assert.DoesNotContain("script-src 'self' 'unsafe-inline'", csp);
    }
}
