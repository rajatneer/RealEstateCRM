using Microsoft.EntityFrameworkCore;
using RealEstateCRM.Data;
using RealEstateCRM.Services;


var builder = WebApplication.CreateBuilder(args);

// For Render.com: listen on the port specified by the PORT environment variable
var port = Environment.GetEnvironmentVariable("PORT") ?? "5133";
builder.WebHost.UseUrls($"http://*:{port}");

// Log4net
builder.Logging.AddLog4Net("log4net.config");

// Database
builder.Services.AddDbContext<CrmDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<ICrmTaskService, CrmTaskService>();
builder.Services.AddScoped<IBrokerageService, BrokerageService>();
builder.Services.AddScoped<ISiteVisitService, SiteVisitService>();
builder.Services.AddSingleton<ICalculatorService, CalculatorService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

var app = builder.Build();

// Auto-migrate database on startup and seed test credentials
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
    db.Database.Migrate();

    // Seed test company and user if not present
    var testCompany = db.Companies.FirstOrDefault(c => c.Code == "Test");
    if (testCompany == null)
    {
        testCompany = new RealEstateCRM.Models.Company { Code = "Test", Name = "Test Company" };
        db.Companies.Add(testCompany);
        db.SaveChanges();
    }
    var testUser = db.Users.FirstOrDefault(u => u.CompanyId == testCompany.Id && u.Username == "Test");
    if (testUser == null)
    {
        var hash = RealEstateCRM.Controllers.AuthController.HashPassword("Test123");
        db.Users.Add(new RealEstateCRM.Models.User
        {
            CompanyId = testCompany.Id,
            Username = "Test",
            PasswordHash = hash,
            IsActive = true
        });
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
