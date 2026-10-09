using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RealEstateCRM.Data;
using RealEstateCRM.Infrastructure;
using RealEstateCRM.Security;
using RealEstateCRM.Services;
using RealEstateCRM.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// Hosting platforms such as Render provide the port through PORT.
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Logging.AddLog4Net("log4net.config");

// ── Database (SQLite for local dev, PostgreSQL via DATABASE_URL / Database:Provider) ──
var (dbProvider, dbConnectionString) = DatabaseConfig.Resolve(builder.Configuration);
builder.Services.AddDbContext<CrmDbContext>(options =>
{
    if (DatabaseConfig.IsPostgres(dbProvider))
        options.UseNpgsql(dbConnectionString);
    else
        options.UseSqlite(dbConnectionString);
});

// ── Authentication / authorization (JWT bearer) ──
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Section));
var jwt = builder.Configuration.GetSection(JwtSettings.Section).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key) && builder.Environment.IsDevelopment())
{
    // Development-only fallback so `dotnet run` works out of the box. Never used in other environments.
    jwt.Key = "dev-only-signing-key-change-me-0123456789";
    builder.Services.PostConfigure<JwtSettings>(o => o.Key = jwt.Key);
}
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Set the Jwt__Key environment variable.");

builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "name",
            RoleClaimType = "role"
        };

        // Reject tokens of users who were disabled, deleted or signed out since the token was issued.
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var validator = context.HttpContext.RequestServices.GetRequiredService<IUserSessionValidator>();

                if (!int.TryParse(principal?.FindFirst("sub")?.Value, out var userId) ||
                    !int.TryParse(principal?.FindFirst("companyId")?.Value, out var companyId) ||
                    !int.TryParse(principal?.FindFirst("ver")?.Value, out var version) ||
                    !await validator.IsValidAsync(userId, companyId, version))
                {
                    context.Fail("The session is no longer valid.");
                }
            }
        };
    });

// Every endpoint requires a signed-in user unless it opts out with [AllowAnonymous].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});

// ── Rate limiting (login brute-force protection) ──
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Behind Render's proxy: trust X-Forwarded-* so client IPs (rate limiting) and scheme are correct.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Errors ──
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ── Application services ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpTenantProvider>();
builder.Services.AddScoped<IPagingContext, HttpPagingContext>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IUserSessionValidator, UserSessionValidator>();

builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<ICrmTaskService, CrmTaskService>();
builder.Services.AddScoped<IBrokerageService, BrokerageService>();
builder.Services.AddScoped<ISiteVisitService, SiteVisitService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var app = builder.Build();

await DbInitializer.InitializeAsync(app.Services, app.Configuration, app.Environment);

app.UseForwardedHeaders();
app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    // Scripts only from our own origin (the frontend has no inline scripts or handlers).
    // Inline style attributes are still used by the markup, hence 'unsafe-inline' for styles only.
    headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; " +
        "font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapControllers();

app.Run();

// Required so integration tests can use WebApplicationFactory<Program>.
public partial class Program { }
