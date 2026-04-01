using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

/// <summary>
/// Handles trade execution with pessimistic locking to prevent double-selling.
///
/// Concurrency strategy:
/// - Uses IsolationLevel.RepeatableRead transaction
/// - Acquires row-level FOR UPDATE locks in a fixed order to prevent deadlocks:
///   1. Sell orders (as a set)
///   2. Buyer credit balance
///   3. Seller asset balances (sorted by TraderId ASC)
///   4. Seller credit balances (sorted by TraderId ASC)
/// - Re-computes fills inside the transaction (never trusts pre-transaction quote)
/// - Full rollback on any failure
/// </summary>
public class TradeExecutionService(IMarketDbContext db) : ITradeExecutionService
{
    public async Task<TradeResponse> ExecuteAsync(ExecuteTradeRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        // Validate buyer and asset type exist (outside transaction — fast read)
        var buyer = await db.Traders.FindAsync([request.BuyerTraderId], ct)
            ?? throw new NotFoundException(nameof(Trader), request.BuyerTraderId);
        if (buyer.Status == TraderStatus.Inactive)
            throw new ValidationException("Inactive traders cannot execute trades.");

        var assetType = await db.AssetTypes.FindAsync([request.AssetTypeId], ct)
            ?? throw new NotFoundException(nameof(AssetType), request.AssetTypeId);
        if (!assetType.IsActive)
            throw new ValidationException($"Asset type '{assetType.Slug}' is not active.");

        // Idempotency fast-path check (outside transaction)
        if (request.IdempotencyKey is not null)
        {
            var existing = await db.Trades
                .Include(t => t.Fills)
                .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
            if (existing is not null)
                return await MapToResponseAsync(existing, assetType.Slug, ct);
        }

        await using var tx = await db.BeginTradeTransactionAsync(ct);
        try
        {
            // Idempotency inner check (inside transaction — handles concurrent same-key requests)
            if (request.IdempotencyKey is not null)
            {
                var existing = await db.Trades
                    .Include(t => t.Fills)
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
                if (existing is not null)
                {
                    await tx.CommitAsync(ct);
                    return await MapToResponseAsync(existing, assetType.Slug, ct);
                }
            }

            // Step 1: Lock sell orders FOR UPDATE (excludes buyer's own orders — anti-wash-trade)
            var lockedOrders = await db.LockSellOrdersForUpdateAsync(request.AssetTypeId, request.BuyerTraderId, ct);

            // Step 2: Compute fills using same algorithm as QuoteService
            var fillResult = FillCalculator.Compute(lockedOrders, request.Quantity);
            if (!fillResult.IsFillable)
                throw new OrderNotFillableException(request.AssetTypeId, request.Quantity, fillResult.AvailableQuantity);

            // Step 3: Lock buyer credit balance FOR UPDATE
            var buyerCredit = await db.LockCreditBalanceForUpdateAsync(request.BuyerTraderId, ct)
                ?? throw new ValidationException($"Trader '{request.BuyerTraderId}' has no credit balance.");

            if (buyerCredit.Credits < fillResult.TotalCost)
                throw new InsufficientCreditsException(request.BuyerTraderId, fillResult.TotalCost, buyerCredit.Credits);

            var now = DateTime.UtcNow;

            // Step 4: Create Trade record first (fills reference it)
            var trade = new Trade
            {
                Id = Guid.NewGuid(),
                BuyerTraderId = request.BuyerTraderId,
                AssetTypeId = request.AssetTypeId,
                RequestedQuantity = request.Quantity,
                TotalQuantity = fillResult.TotalQuantity,
                TotalCost = fillResult.TotalCost,
                AverageUnitPrice = fillResult.AverageUnitPrice,
                ExecutedAt = now,
                IdempotencyKey = request.IdempotencyKey
            };
            db.Trades.Add(trade);

            // Step 5: Process fills sorted by TraderId ASC (deadlock prevention)
            var sortedFills = fillResult.Items.OrderBy(f => f.SellOrder.TraderId).ToList();

            foreach (var fill in sortedFills)
            {
                var order = fill.SellOrder;

                // Lock seller asset balance FOR UPDATE
                var sellerAsset = await db.LockAssetBalanceForUpdateAsync(order.TraderId, request.AssetTypeId, ct)
                    ?? throw new ValidationException($"Seller '{order.TraderId}' has no asset balance for '{request.AssetTypeId}'.");

                // Lock seller credit balance FOR UPDATE
                var sellerCredit = await db.LockCreditBalanceForUpdateAsync(order.TraderId, ct)
                    ?? throw new ValidationException($"Seller '{order.TraderId}' has no credit balance.");

                // Mutate sell order
                order.RemainingQuantity -= fill.Quantity;
                order.Status = order.RemainingQuantity == 0 ? SellOrderStatus.Completed : SellOrderStatus.PartiallyFilled;
                order.UpdatedAt = now;

                // Transfer asset from seller (remove from total + reserved)
                sellerAsset.TotalQuantity -= fill.Quantity;
                sellerAsset.ReservedQuantity -= fill.Quantity;

                // Credit seller
                sellerCredit.Credits += fill.SubTotal;

                // Create fill record
                var tradeFill = new TradeFill
                {
                    Id = Guid.NewGuid(),
                    TradeId = trade.Id,
                    SellOrderId = order.Id,
                    SellerTraderId = order.TraderId,
                    AssetTypeId = request.AssetTypeId,
                    Quantity = fill.Quantity,
                    UnitPrice = fill.UnitPrice,
                    SubTotal = fill.SubTotal,
                    ExecutedAt = now
                };
                db.TradeFills.Add(tradeFill);

                // Asset transfer record
                db.AssetTransfers.Add(new AssetTransfer
                {
                    Id = Guid.NewGuid(),
                    FromTraderId = order.TraderId,
                    ToTraderId = request.BuyerTraderId,
                    AssetTypeId = request.AssetTypeId,
                    Quantity = fill.Quantity,
                    TradeId = trade.Id,
                    CreatedAt = now
                });

                // Credit transfer (seller receives payment)
                db.CreditTransfers.Add(new CreditTransfer
                {
                    Id = Guid.NewGuid(),
                    FromTraderId = request.BuyerTraderId,
                    ToTraderId = order.TraderId,
                    Amount = fill.SubTotal,
                    TradeId = trade.Id,
                    CreatedAt = now
                });

                // Ledger entries for seller
                db.LedgerEntries.Add(new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TraderId = order.TraderId,
                    AssetTypeId = request.AssetTypeId,
                    TradeId = trade.Id,
                    SellOrderId = order.Id,
                    EntryType = LedgerEntryType.TradeSellerAssetDebit,
                    QuantityDelta = -fill.Quantity,
                    CreatedAt = now
                });
                db.LedgerEntries.Add(new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TraderId = order.TraderId,
                    TradeId = trade.Id,
                    SellOrderId = order.Id,
                    EntryType = LedgerEntryType.TradeSellerCreditCredit,
                    CreditDelta = fill.SubTotal,
                    CreatedAt = now
                });

                if (order.Status == SellOrderStatus.Completed)
                {
                    db.LedgerEntries.Add(new LedgerEntry
                    {
                        Id = Guid.NewGuid(),
                        TraderId = order.TraderId,
                        AssetTypeId = request.AssetTypeId,
                        SellOrderId = order.Id,
                        EntryType = LedgerEntryType.OrderCompleted,
                        CreatedAt = now
                    });
                }
            }

            // Step 6: Debit buyer credits
            buyerCredit.Credits -= fillResult.TotalCost;

            // Step 7: Credit buyer asset balance (create row if first purchase)
            var buyerAsset = await db.GetOrCreateAssetBalanceAsync(request.BuyerTraderId, request.AssetTypeId, ct);
            buyerAsset.TotalQuantity += fillResult.TotalQuantity;

            // Ledger entries for buyer
            db.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TraderId = request.BuyerTraderId,
                TradeId = trade.Id,
                EntryType = LedgerEntryType.TradeBuyerCreditDebit,
                CreditDelta = -fillResult.TotalCost,
                CreatedAt = now
            });
            db.LedgerEntries.Add(new LedgerEntry
            {
                Id = Guid.NewGuid(),
                TraderId = request.BuyerTraderId,
                AssetTypeId = request.AssetTypeId,
                TradeId = trade.Id,
                EntryType = LedgerEntryType.TradeBuyerAssetCredit,
                QuantityDelta = fillResult.TotalQuantity,
                CreatedAt = now
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            trade.Fills = sortedFills.Select((f, i) => db.TradeFills.Local
                .First(tf => tf.SellOrderId == f.SellOrder.Id && tf.TradeId == trade.Id)).ToList();

            return await MapToResponseAsync(trade, assetType.Slug, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex, "ix_trades_idempotency_key"))
        {
            await tx.RollbackAsync(ct);
            // Another concurrent request with same idempotency key committed first
            var existing = await db.Trades
                .Include(t => t.Fills)
                .FirstAsync(t => t.IdempotencyKey == request.IdempotencyKey, ct);
            return await MapToResponseAsync(existing, assetType.Slug, ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<TradeResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var trade = await db.Trades
            .Include(t => t.Fills)
            .Include(t => t.AssetType)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new NotFoundException(nameof(Trade), id);

        return await MapToResponseAsync(trade, trade.AssetType!.Slug, ct);
    }

    public async Task<PaginatedResponse<TradeResponse>> ListAsync(
        Guid? assetTypeId, Guid? buyerTraderId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Trades
            .Include(t => t.Fills)
            .Include(t => t.AssetType)
            .AsQueryable();

        if (assetTypeId.HasValue) query = query.Where(t => t.AssetTypeId == assetTypeId);
        if (buyerTraderId.HasValue) query = query.Where(t => t.BuyerTraderId == buyerTraderId);

        query = query.OrderByDescending(t => t.ExecutedAt);
        var total = await query.CountAsync(ct);
        var trades = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var responses = new List<TradeResponse>();
        foreach (var t in trades)
            responses.Add(await MapToResponseAsync(t, t.AssetType!.Slug, ct));

        return new PaginatedResponse<TradeResponse>(responses, total, page, pageSize);
    }

    public async Task<List<TradeFillResponse>> GetFillsAsync(Guid tradeId, CancellationToken ct = default)
    {
        if (!await db.Trades.AnyAsync(t => t.Id == tradeId, ct))
            throw new NotFoundException(nameof(Trade), tradeId);

        return await db.TradeFills
            .Where(f => f.TradeId == tradeId)
            .OrderBy(f => f.ExecutedAt)
            .Select(f => new TradeFillResponse(
                f.Id, f.SellOrderId, f.SellerTraderId,
                f.Quantity, f.UnitPrice, f.SubTotal, f.ExecutedAt))
            .ToListAsync(ct);
    }

    private static Task<TradeResponse> MapToResponseAsync(Trade t, string assetCode, CancellationToken ct)
    {
        var fills = t.Fills
            .Select(f => new TradeFillResponse(
                f.Id, f.SellOrderId, f.SellerTraderId,
                f.Quantity, f.UnitPrice, f.SubTotal, f.ExecutedAt))
            .ToList();

        return Task.FromResult(new TradeResponse(
            t.Id, t.BuyerTraderId, t.AssetTypeId, assetCode,
            t.RequestedQuantity, t.TotalQuantity, t.TotalCost, t.AverageUnitPrice,
            t.ExecutedAt, t.IdempotencyKey, fills));
    }

    private static bool IsUniqueViolation(DbUpdateException ex, string indexName)
    {
        return ex.InnerException?.Message.Contains("23505", StringComparison.Ordinal) == true
            || ex.InnerException?.Message.Contains(indexName, StringComparison.Ordinal) == true;
    }
}
