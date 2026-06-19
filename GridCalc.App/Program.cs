using GridCalc.App.Components;
using GridCalc.App.CryptoExchange;
using GridCalc.App.Data;
using GridCalc.App.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration: User Secrets (local dev) → Environment Variables (CI/CD) → appsettings.json
// In GitHub Actions, set secrets as env vars using double underscore for nested keys (e.g. MyApi__Key)
builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddDbContextFactory<GridCalcDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gridcalc.db"));

builder.Services.AddSingleton<ExchangeRepository>();
builder.Services.AddSingleton<IExchange, BinanceExchange>();

builder.Services.AddHostedService<TradeCollectorService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();