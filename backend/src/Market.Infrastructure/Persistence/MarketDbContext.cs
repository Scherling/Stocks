using System.Data;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Market.Infrastructure.Persistence;

public class MarketDbContext : DbContext, IMarketDbContext
{
    public MarketDbContext(DbContextOptions<MarketDbContext> options) : base(options) { }

    public DbSet<Trader> Traders => Set<Trader>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<TraderAssetBalance> TraderAssetBalances => Set<TraderAssetBalance>();
    public DbSet<TraderCreditBalance> TraderCreditBalances => Set<TraderCreditBalance>();
    public DbSet<SellOrder> SellOrders => Set<SellOrder>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<TradeFill> TradeFills => Set<TradeFill>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<AssetTransfer> AssetTransfers => Set<AssetTransfer>();
    public DbSet<CreditTransfer> CreditTransfers => Set<CreditTransfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MarketDbContext).Assembly);
    }

    /// <inheritdoc />
    public virtual Task<IDbContextTransaction> BeginTradeTransactionAsync(CancellationToken ct = default)
        => Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

    /// <summary>
    /// Locks open sell orders FOR UPDATE within an active RepeatableRead transaction.
    /// Orders are returned sorted by unit_price ASC, created_at ASC (FIFO at same price).
    /// Buyer's own orders are excluded to prevent wash trading.
    ///
    /// IMPORTANT: Do NOT pre-load these entities before calling this method — EF identity map
    /// may return stale cached instances if the same rows were already fetched in this DbContext scope.
    /// </summary>
    public virtual async Task<List<SellOrder>> LockSellOrdersForUpdateAsync(
        Guid assetTypeId, Guid buyerTraderId, CancellationToken ct = default)
    {
        return await SellOrders
            .FromSqlRaw(
                "SELECT * FROM sell_orders WHERE asset_type_id = @assetTypeId AND status IN ('Open', 'PartiallyFilled') AND trader_id != @buyerTraderId ORDER BY unit_price ASC, created_at ASC FOR UPDATE",
                new Npgsql.NpgsqlParameter("assetTypeId", assetTypeId),
                new Npgsql.NpgsqlParameter("buyerTraderId", buyerTraderId))
            .AsTracking()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Locks a trader's credit balance row FOR UPDATE within an active transaction.
    /// Returns null if the trader has no credit balance row.
    /// </summary>
    public virtual async Task<TraderCreditBalance?> LockCreditBalanceForUpdateAsync(
        Guid traderId, CancellationToken ct = default)
    {
        var results = await TraderCreditBalances
            .FromSqlRaw(
                "SELECT * FROM trader_credit_balances WHERE trader_id = @traderId FOR UPDATE",
                new Npgsql.NpgsqlParameter("traderId", traderId))
            .AsTracking()
            .ToListAsync(ct);

        return results.FirstOrDefault();
    }

    /// <summary>
    /// Locks a trader's asset balance row FOR UPDATE within an active transaction.
    /// Returns null if no balance row exists for the given trader+asset combination.
    /// </summary>
    public virtual async Task<TraderAssetBalance?> LockAssetBalanceForUpdateAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default)
    {
        var results = await TraderAssetBalances
            .FromSqlRaw(
                "SELECT * FROM trader_asset_balances WHERE trader_id = @traderId AND asset_type_id = @assetTypeId FOR UPDATE",
                new Npgsql.NpgsqlParameter("traderId", traderId),
                new Npgsql.NpgsqlParameter("assetTypeId", assetTypeId))
            .AsTracking()
            .ToListAsync(ct);

        return results.FirstOrDefault();
    }

    /// <summary>
    /// Gets or creates a buyer asset balance row inside an active transaction.
    /// Used for first-time buyers of an asset who have no existing balance row.
    /// </summary>
    public virtual async Task<TraderAssetBalance> GetOrCreateAssetBalanceAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default)
    {
        // Try to find in local change tracker first (may have been locked already)
        var local = TraderAssetBalances.Local
            .FirstOrDefault(b => b.TraderId == traderId && b.AssetTypeId == assetTypeId);
        if (local is not null) return local;

        // Try to lock from DB
        var existing = await LockAssetBalanceForUpdateAsync(traderId, assetTypeId, ct);
        if (existing is not null) return existing;

        // First-time buyer — create a new balance row
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
