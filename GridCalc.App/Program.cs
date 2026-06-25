using System.Collections.Concurrent;
using GridCalc.App.Components;
using GridCalc.App.CryptoExchange;
using GridCalc.App.Data;
using GridCalc.App.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Syncfusion.Blazor;

Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1JHaF5cWWdCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdlWXlccHZVRmhfWUB+WEFWYEo=");

var builder = WebApplication.CreateBuilder(args);

// Configuration: User Secrets → Environment Variables → appsettings.json
builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Configure Serilog (file + console)
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});


// Add services
// builder.Services.AddDbContextFactory<GridCalcDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gridcalc.db"));

builder.Services.AddDbContextFactory<GridCalcDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=localhost;Database=GridCalc;Trusted_Connection=True;TrustServerCertificate=True"));

builder.Services.AddSyncfusionBlazor();

builder.Services.AddSingleton<Candle1MinutesRepository>();
builder.Services.AddSingleton<ExchangeRepository>();
builder.Services.AddSingleton<SymbolRepository>();
builder.Services.AddSingleton<TradeRepository>();
builder.Services.AddSingleton<IExchange, BinanceExchange>();
builder.Services.AddSingleton<GridTradeCache>();
builder.Services.AddSingleton<ConcurrentDictionary<string, CandleCache1Minutes>>();

builder.Services.AddHostedService<TradeCollectorService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Create DB if not exists and apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GridCalcDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    var conn = db.Database.GetDbConnection();

    logger.LogInformation("EF Core connection string: {ConnectionString}", conn.ConnectionString);
    logger.LogInformation("Database: {Database}", conn.Database);
    logger.LogInformation("DataSource: {DataSource}", conn.DataSource);
    // logger.LogInformation("ServerVersion: {ServerVersion}", conn.ServerVersion);

    db.Database.Migrate();
}


// Middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();