using Market.Infrastructure.Persistence;
using Market.Infrastructure.Persistence.Seed;

namespace Stocks.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin");

        group.MapPost("/reseed", async (
            MarketDbContext db,
            ILogger<Program> logger) =>
        {
            var jsonPath = Path.Combine(AppContext.BaseDirectory, "Data", "Resources.json");
            if (!File.Exists(jsonPath))
                return Results.Problem($"Resources.json not found at: {jsonPath}", statusCode: 500);

            var resources = DataSeeder.LoadResources(jsonPath);
            await DataSeeder.ReseedAsync(db, logger, resources);
            return Results.Ok(new { message = "Reseed complete.", assetTypeCount = resources.Count });
        })
        .WithName("ReseedDatabase")
        .WithSummary("Wipe all market data and re-seed from Resources.json")
        .Produces<object>()
        .ProducesProblem(500);
    }
}
