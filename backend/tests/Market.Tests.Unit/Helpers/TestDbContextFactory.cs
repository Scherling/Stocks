using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Market.Tests.Unit.Helpers;

/// <summary>
/// Creates an in-memory MarketDbContext for unit tests.
/// Lock methods return plain query results (no actual FOR UPDATE SQL).
/// </summary>
public class TestMarketDbContext : MarketDbContext
{
    public TestMarketDbContext(DbContextOptions<MarketDbContext> options) : base(options) { }

    public override async Task<List<SellOrder>> LockSellOrdersForUpdateAsync(
        Guid assetTypeId, Guid buyerTraderId, CancellationToken ct = default)
    {
        return await SellOrders
            .Where(o => o.AssetTypeId == assetTypeId
                && (o.Status == Domain.Enums.SellOrderStatus.Open || o.Status == Domain.Enums.SellOrderStatus.PartiallyFilled)
                && o.TraderId != buyerTraderId)
            .OrderBy(o => o.UnitPrice)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(ct);
    }

    public override async Task<TraderCreditBalance?> LockCreditBalanceForUpdateAsync(
        Guid traderId, CancellationToken ct = default)
    {
        return await TraderCreditBalances.FirstOrDefaultAsync(b => b.TraderId == traderId, ct);
    }

    public override async Task<TraderAssetBalance?> LockAssetBalanceForUpdateAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default)
    {
        return await TraderAssetBalances
            .FirstOrDefaultAsync(b => b.TraderId == traderId && b.AssetTypeId == assetTypeId, ct);
    }

    // In-memory provider doesn't support real transactions — return a no-op transaction
    public override Task<IDbContextTransaction> BeginTradeTransactionAsync(CancellationToken ct = default)
        => Database.BeginTransactionAsync(ct);

    public override async Task<TraderAssetBalance> GetOrCreateAssetBalanceAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default)
    {
        var local = TraderAssetBalances.Local
            .FirstOrDefault(b => b.TraderId == traderId && b.AssetTypeId == assetTypeId);
        if (local is not null) return local;

        var existing = await TraderAssetBalances
            .FirstOrDefaultAsync(b => b.TraderId == traderId && b.AssetTypeId == assetTypeId, ct);
        if (existing is not null) return existing;

        var balance = new TraderAssetBalance
        {
            Id = Guid.NewGuid(),
            TraderId = traderId,
            AssetTypeId = assetTypeId,
            TotalQuantity = 0m,
            ReservedQuantity = 0m
        };
        TraderAssetBalances.Add(balance);
        return balance;
    }
}

public static class TestDbContextFactory
{
    public static TestMarketDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<MarketDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new TestMarketDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
