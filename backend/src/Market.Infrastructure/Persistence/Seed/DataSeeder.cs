using System.Text.Json;
using System.Text.Json.Serialization;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Market.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Market.Infrastructure.Persistence.Seed;

public record ResourceEntry(
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("Name")]        string Name,
    [property: JsonPropertyName("Category")]    string Category,
    [property: JsonPropertyName("Stage")]       string Stage,
    [property: JsonPropertyName("Description")] string Description);

/// <summary>
/// Idempotent seed data for development/testing.
/// Normal SeedAsync only runs if no traders exist.
/// ReseedAsync wipes all data first, then re-seeds.
/// </summary>
public static class DataSeeder
{
    // Slugs for the 10 commodities used in trader balances / sell orders
    private static readonly HashSet<string> _seedCommodities = new(StringComparer.OrdinalIgnoreCase)
    {
        "coal", "iron-ore", "grain", "raw-cotton", "pig-iron",
        "cotton-yarn", "machine-tools", "cotton-fabrics", "basic-hand-tools", "coal-tar"
    };

    public static IReadOnlyList<ResourceEntry> LoadResources(string jsonPath)
    {
        using var stream = File.OpenRead(jsonPath);
        return JsonSerializer.Deserialize<List<ResourceEntry>>(stream)
            ?? throw new InvalidOperationException($"Failed to deserialize {jsonPath}");
    }

    public static async Task SeedAsync(MarketDbContext db, ILogger logger, IReadOnlyList<ResourceEntry> resources)
    {
        if (await db.Traders.AnyAsync())
        {
            logger.LogInformation("Seed data already exists — skipping.");
            return;
        }

        await DoSeedAsync(db, logger, resources);
    }

    public static async Task ReseedAsync(MarketDbContext db, ILogger logger, IReadOnlyList<ResourceEntry> resources)
    {
        logger.LogInformation("Reseeding: removing all existing market data...");

        db.LedgerEntries.RemoveRange(db.LedgerEntries);
        db.SellOrders.RemoveRange(db.SellOrders);
        db.TraderAssetBalances.RemoveRange(db.TraderAssetBalances);
        db.TraderCreditBalances.RemoveRange(db.TraderCreditBalances);
        db.Traders.RemoveRange(db.Traders);
        db.AssetTypes.RemoveRange(db.AssetTypes);
        await db.SaveChangesAsync();

        await DoSeedAsync(db, logger, resources);
    }

    // ── Private implementation ─────────────────────────────────────────────────

    private static async Task DoSeedAsync(MarketDbContext db, ILogger logger, IReadOnlyList<ResourceEntry> resources)
    {
        logger.LogInformation("Seeding initial market data from {Count} resource definitions...", resources.Count);

        var now = DateTime.UtcNow;

        // ── Asset types (one per resource entry) ──────────────────────────────
        var assetTypes = resources.Select(r => new AssetType
        {
            Id          = Guid.NewGuid(),
            Slug        = r.Id,
            Name        = r.Name,
            Category    = r.Category,
            Stage       = r.Stage,
            Description = r.Description,
            UnitName    = DeriveUnitName(r.Id),
            IsActive    = true,
            CreatedAt   = now,
            UpdatedAt   = now,
        }).ToList();

        db.AssetTypes.AddRange(assetTypes);
        await db.SaveChangesAsync();

        // Build lookup by slug id for use below
        var assetById = assetTypes.ToDictionary(a => a.Slug);

        // ── Traders ───────────────────────────────────────────────────────────
        var alice = MakeTrader("Alice", now);
        var bob   = MakeTrader("Bob",   now);
        var carol = MakeTrader("Carol", now);
        var dave  = MakeTrader("Dave",  now);
        var eve   = MakeTrader("Eve",   now);
        db.Traders.AddRange(alice, bob, carol, dave, eve);

        // ── Credit balances ───────────────────────────────────────────────────
        db.TraderCreditBalances.AddRange(
            MakeCredit(alice.Id, 10_000m),
            MakeCredit(bob.Id,   10_000m),
            MakeCredit(carol.Id, 10_000m),
            MakeCredit(dave.Id,  20_000m),
            MakeCredit(eve.Id,   20_000m));

        // ── Asset balances for sellers (Alice, Bob, Carol) ────────────────────
        var seedAssets = _seedCommodities
            .Where(assetById.ContainsKey)
            .Select(id => assetById[id])
            .ToList();

        foreach (var trader in new[] { alice, bob, carol })
        foreach (var asset in seedAssets)
        {
            db.TraderAssetBalances.Add(new TraderAssetBalance
            {
                Id               = Guid.NewGuid(),
                TraderId         = trader.Id,
                AssetTypeId      = asset.Id,
                TotalQuantity    = 500m,
                ReservedQuantity = 0m
            });
        }

        await db.SaveChangesAsync();

        // ── Sell orders ───────────────────────────────────────────────────────
        // Each of the 10 commodities gets 3 orders (one per seller), at varied prices.
        var priceVariants = new[] { (0.0, 1.0m), (1.0, 1.15m), (2.0, 0.90m) }; // (seconds offset, price multiplier)
        var basePrices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["coal"]             = 4.00m,
            ["iron-ore"]         = 8.00m,
            ["grain"]            = 5.00m,
            ["raw-cotton"]       = 9.00m,
            ["pig-iron"]         = 15.00m,
            ["cotton-yarn"]      = 12.00m,
            ["machine-tools"]    = 40.00m,
            ["cotton-fabrics"]   = 18.00m,
            ["basic-hand-tools"] = 22.00m,
            ["coal-tar"]         = 3.50m,
        };

        var sellers = new[] { alice, bob, carol };
        var orders  = new List<SellOrder>();

        foreach (var asset in seedAssets)
        {
            var basePrice = basePrices.TryGetValue(asset.Slug, out var bp) ? bp : 10m;
            for (int i = 0; i < sellers.Length; i++)
            {
                var (secOffset, mult) = priceVariants[i];
                orders.Add(MakeOrder(sellers[i].Id, asset.Id,
                    qty:   100m,
                    price: Math.Round(basePrice * mult, 2),
                    now:   now.AddSeconds(secOffset)));
            }
        }

        // Update reserved quantities
        foreach (var order in orders)
        {
            var balance = await db.TraderAssetBalances
                .FirstAsync(b => b.TraderId == order.TraderId && b.AssetTypeId == order.AssetTypeId);
            balance.ReservedQuantity += order.OriginalQuantity;
        }

        db.SellOrders.AddRange(orders);
        await db.SaveChangesAsync();

        // ── Ledger entries for order creations ────────────────────────────────
        db.LedgerEntries.AddRange(orders.Select(o => new LedgerEntry
        {
            Id            = Guid.NewGuid(),
            TraderId      = o.TraderId,
            AssetTypeId   = o.AssetTypeId,
            SellOrderId   = o.Id,
            EntryType     = LedgerEntryType.SellOrderCreatedReservation,
            QuantityDelta = -o.OriginalQuantity,
            CreatedAt     = o.CreatedAt
        }));
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Seed complete: {TraderCount} traders, {AssetCount} asset types, {OrderCount} sell orders.",
            5, assetTypes.Count, orders.Count);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string DeriveUnitName(string slug) => slug switch
    {
        "coal" or "coke" or "limestone" or "salt" or "saltpeter" or "silica-sand"
            or "sulfur" or "timber" or "phosphate-rock"                          => "ton",
        "iron-ore" or "copper-ore" or "silver-ore"                               => "ton",
        "grain" or "raw-cotton" or "raw-hemp" or "raw-wool" or "natural-rubber" or "flax" => "bale",
        "cotton-yarn" or "wool-yarn" or "flax-yarn"                              => "bolt",
        "pig-iron" or "copper-ingots" or "steel-ingots"                          => "ingot",
        _                                                                         => "unit",
    };

    private static Trader MakeTrader(string name, DateTime now) => new()
    {
        Id        = Guid.NewGuid(),
        Name      = name,
        Status    = TraderStatus.Active,
        CreatedAt = now,
        UpdatedAt = now
    };

    private static TraderCreditBalance MakeCredit(Guid traderId, decimal credits) => new()
    {
        Id       = Guid.NewGuid(),
        TraderId = traderId,
        Credits  = credits
    };

    private static SellOrder MakeOrder(Guid traderId, Guid assetTypeId, decimal qty, decimal price, DateTime now) => new()
    {
        Id               = Guid.NewGuid(),
        TraderId         = traderId,
        AssetTypeId      = assetTypeId,
        OriginalQuantity = qty,
        RemainingQuantity = qty,
        UnitPrice        = price,
        Status           = SellOrderStatus.Open,
        CreatedAt        = now,
        UpdatedAt        = now
    };
}
