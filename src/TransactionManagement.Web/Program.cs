using System.Text;
using Microsoft.AspNetCore.Mvc;
using TransactionManagement.Application;
using TransactionManagement.Infrastructure;
using TransactionManagement.Infrastructure.Persistence;
using TransactionManagement.Web.Configuration;
using TransactionManagement.Web.Infrastructure;
using TransactionManagement.Web.Reporting;

var builder = WebApplication.CreateBuilder(args);

// The RDLC engine reads legacy code pages; the provider must be registered before first use.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services
    .AddOptions<ReportSettings>()
    .Bind(builder.Configuration.GetSection(ReportSettings.SectionName));

builder.Services
    .AddOptions<CompanySettings>()
    .Bind(builder.Configuration.GetSection(CompanySettings.SectionName));

builder.Services.AddScoped<IRdlcReportRenderer, RdlcReportRenderer>();

builder.Services.AddControllersWithViews(options =>
{
    // Anti-forgery validation is on by default for every unsafe verb, so a missing
    // [ValidateAntiForgeryToken] can never silently leave an action unprotected.
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddAntiforgery(options => options.HeaderName = "RequestVerificationToken");

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

var app = builder.Build();

// Centralised error handling: the handler classifies and logs, the error endpoint renders.
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Transactions}/{action=Index}/{id?}");

await InitialiseDatabaseAsync(app);

await app.RunAsync();

// Applies migrations and seeds sample data. Guarded by configuration so it never runs
// unintentionally against a production database.
static async Task InitialiseDatabaseAsync(WebApplication application)
{
    var applyMigrations = application.Configuration
        .GetValue("DatabaseInitialisation:ApplyMigrations", application.Environment.IsDevelopment());

    var seedSampleData = application.Configuration
        .GetValue("DatabaseInitialisation:SeedSampleData", application.Environment.IsDevelopment());

    if (!applyMigrations && !seedSampleData)
    {
        return;
    }

    await using var scope = application.Services.CreateAsyncScope();

    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

    if (applyMigrations)
    {
        await initialiser.MigrateAsync();
    }

    if (seedSampleData)
    {
        await initialiser.SeedAsync();
    }
}

/// <summary>Exposed so the integration test project can reference the entry-point assembly.</summary>
public partial class Program;
