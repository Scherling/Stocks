using Market.Application.Interfaces;
using Market.Application.Services;
using Market.Infrastructure.Persistence;
using Market.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Stocks.Api.Endpoints;
using Stocks.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ── Database ─────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<MarketDbContext>(options =>
    options.UseNpgsql(connectionString)
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IMarketDbContext>(sp => sp.GetRequiredService<MarketDbContext>());

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<ITraderService, TraderService>();
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<ISellOrderService, SellOrderService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddScoped<ITradeExecutionService, TradeExecutionService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IMarketAnalyticsService, MarketAnalyticsService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// ── OpenAPI ───────────────────────────────────────────────────────────────────
builder.Services.AddOpenApi();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

// ── OpenAPI / Scalar UI ───────────────────────────────────────────────────────
app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Market API";
    options.Theme = ScalarTheme.Mars;
});

// ── Health check ──────────────────────────────────────────────────────────────
app.MapHealthChecks("/health");

// ── API Endpoints ─────────────────────────────────────────────────────────────
app.MapTraders();
app.MapAssetTypes();
app.MapSellOrders();
app.MapQuotes();
app.MapTrades();
app.MapLedger();
app.MapMarket();
app.MapAdmin();

// ── Database migrations + seed ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MarketDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully.");

        if (app.Environment.IsDevelopment())
        {
            var resourcesPath = Path.Combine(AppContext.BaseDirectory, "Data", "Resources.json");
            var resources = DataSeeder.LoadResources(resourcesPath);
            await DataSeeder.SeedAsync(db, logger, resources);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to apply database migrations.");
        throw;
    }
}

app.Run();

// Needed for integration test project access
public partial class Program { }
