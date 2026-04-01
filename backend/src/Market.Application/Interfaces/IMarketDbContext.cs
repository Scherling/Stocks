using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Market.Application.Interfaces;

public interface IMarketDbContext
{
    DbSet<Trader> Traders { get; }
    DbSet<AssetType> AssetTypes { get; }
    DbSet<TraderAssetBalance> TraderAssetBalances { get; }
    DbSet<TraderCreditBalance> TraderCreditBalances { get; }
    DbSet<SellOrder> SellOrders { get; }
    DbSet<Trade> Trades { get; }
    DbSet<TradeFill> TradeFills { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<AssetTransfer> AssetTransfers { get; }
    DbSet<CreditTransfer> CreditTransfers { get; }

    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a RepeatableRead transaction for trade execution.
    /// Using RepeatableRead is sufficient when every touched row is explicitly locked via FOR UPDATE.
    /// </summary>
    Task<IDbContextTransaction> BeginTradeTransactionAsync(CancellationToken ct = default);

    // Pessimistic lock methods — lock rows FOR UPDATE within an active transaction.
    // IMPORTANT: Always call these inside a transaction. Do not pre-load these entities before calling.
    Task<List<SellOrder>> LockSellOrdersForUpdateAsync(
        Guid assetTypeId, Guid buyerTraderId, CancellationToken ct = default);

    Task<TraderCreditBalance?> LockCreditBalanceForUpdateAsync(
        Guid traderId, CancellationToken ct = default);

    Task<TraderAssetBalance?> LockAssetBalanceForUpdateAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default);

    Task<TraderAssetBalance> GetOrCreateAssetBalanceAsync(
        Guid traderId, Guid assetTypeId, CancellationToken ct = default);
}
