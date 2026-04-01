using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class SellOrderService(IMarketDbContext db) : ISellOrderService
{
    public async Task<PaginatedResponse<SellOrderResponse>> ListAsync(
        Guid? assetTypeId, Guid? traderId, SellOrderStatus? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.SellOrders
            .Include(o => o.Trader)
            .Include(o => o.AssetType)
            .AsQueryable();

        if (assetTypeId.HasValue) query = query.Where(o => o.AssetTypeId == assetTypeId);
        if (traderId.HasValue) query = query.Where(o => o.TraderId == traderId);
        if (status.HasValue) query = query.Where(o => o.Status == status);

        query = query.OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => MapToResponse(o))
            .ToListAsync(ct);

        return new PaginatedResponse<SellOrderResponse>(items, total, page, pageSize);
    }

    public async Task<SellOrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.SellOrders
            .Include(o => o.Trader)
            .Include(o => o.AssetType)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(SellOrder), id);

        return MapToResponse(order);
    }

    public async Task<SellOrderResponse> CreateAsync(CreateSellOrderRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");
        if (request.UnitPrice <= 0)
            throw new ValidationException("Unit price must be greater than zero.");

        var trader = await db.Traders.FindAsync([request.TraderId], ct)
            ?? throw new NotFoundException(nameof(Trader), request.TraderId);
        if (trader.Status == TraderStatus.Inactive)
            throw new ValidationException("Inactive traders cannot create sell orders.");

        var assetType = await db.AssetTypes.FindAsync([request.AssetTypeId], ct)
            ?? throw new NotFoundException(nameof(AssetType), request.AssetTypeId);
        if (!assetType.IsActive)
            throw new ValidationException($"Asset type '{assetType.Slug}' is not active.");

        var balance = await db.TraderAssetBalances
            .FirstOrDefaultAsync(b => b.TraderId == request.TraderId && b.AssetTypeId == request.AssetTypeId, ct);

        var available = balance?.AvailableQuantity ?? 0m;
        if (available < request.Quantity)
            throw new InsufficientInventoryException(request.TraderId, request.AssetTypeId, request.Quantity, available);

        // Reserve inventory
        balance!.ReservedQuantity += request.Quantity;

        var now = DateTime.UtcNow;
        var order = new SellOrder
        {
            Id = Guid.NewGuid(),
            TraderId = request.TraderId,
            AssetTypeId = request.AssetTypeId,
            OriginalQuantity = request.Quantity,
            RemainingQuantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            Status = SellOrderStatus.Open,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.SellOrders.Add(order);
        db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TraderId = request.TraderId,
            AssetTypeId = request.AssetTypeId,
            SellOrderId = order.Id,
            EntryType = LedgerEntryType.SellOrderCreatedReservation,
            QuantityDelta = -request.Quantity,
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);

        order.Trader = trader;
        order.AssetType = assetType;
        return MapToResponse(order);
    }

    public async Task<SellOrderResponse> UpdateAsync(Guid id, UpdateSellOrderRequest request, CancellationToken ct = default)
    {
        var order = await db.SellOrders
            .Include(o => o.Trader)
            .Include(o => o.AssetType)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(SellOrder), id);

        if (order.Status == SellOrderStatus.Completed || order.Status == SellOrderStatus.Cancelled)
            throw new ValidationException($"Cannot update a {order.Status.ToString().ToLower()} order.");

        var now = DateTime.UtcNow;

        if (request.UnitPrice.HasValue)
        {
            if (request.UnitPrice.Value <= 0)
                throw new ValidationException("Unit price must be greater than zero.");
            order.UnitPrice = request.UnitPrice.Value;
        }

        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value <= 0)
                throw new ValidationException("Quantity must be greater than zero.");

            var filledQty = order.FilledQuantity;
            if (request.Quantity.Value < filledQty)
                throw new ValidationException($"New quantity ({request.Quantity.Value}) cannot be less than already filled quantity ({filledQty}).");

            var newRemainingQty = request.Quantity.Value - filledQty;
            var delta = newRemainingQty - order.RemainingQuantity; // positive = increase, negative = decrease

            var balance = await db.TraderAssetBalances
                .FirstOrDefaultAsync(b => b.TraderId == order.TraderId && b.AssetTypeId == order.AssetTypeId, ct)
                ?? throw new NotFoundException("TraderAssetBalance", $"{order.TraderId}/{order.AssetTypeId}");

            if (delta > 0)
            {
                // Increasing — need additional available inventory
                if (balance.AvailableQuantity < delta)
                    throw new InsufficientInventoryException(order.TraderId, order.AssetTypeId, delta, balance.AvailableQuantity);
                balance.ReservedQuantity += delta;
            }
            else if (delta < 0)
            {
                // Decreasing — release excess reservation
                balance.ReservedQuantity += delta; // delta is negative
            }

            order.OriginalQuantity = request.Quantity.Value;
            order.RemainingQuantity = newRemainingQty;
            order.Status = newRemainingQty == 0 ? SellOrderStatus.Completed : order.Status;

            if (delta != 0)
            {
                db.LedgerEntries.Add(new LedgerEntry
                {
                    Id = Guid.NewGuid(),
                    TraderId = order.TraderId,
                    AssetTypeId = order.AssetTypeId,
                    SellOrderId = order.Id,
                    EntryType = LedgerEntryType.SellOrderReservationReleased,
                    QuantityDelta = -delta,
                    Metadata = $"Order quantity updated from {order.OriginalQuantity} to {request.Quantity.Value}",
                    CreatedAt = now
                });
            }
        }

        order.UpdatedAt = now;
        await db.SaveChangesAsync(ct);
        return MapToResponse(order);
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var order = await db.SellOrders
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(SellOrder), id);

        if (order.Status == SellOrderStatus.Completed || order.Status == SellOrderStatus.Cancelled)
            throw new ValidationException($"Cannot cancel a {order.Status.ToString().ToLower()} order.");

        var balance = await db.TraderAssetBalances
            .FirstOrDefaultAsync(b => b.TraderId == order.TraderId && b.AssetTypeId == order.AssetTypeId, ct)
            ?? throw new NotFoundException("TraderAssetBalance", $"{order.TraderId}/{order.AssetTypeId}");

        // Release remaining reserved quantity
        balance.ReservedQuantity -= order.RemainingQuantity;

        var now = DateTime.UtcNow;
        order.Status = SellOrderStatus.Cancelled;
        order.UpdatedAt = now;

        db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            TraderId = order.TraderId,
            AssetTypeId = order.AssetTypeId,
            SellOrderId = order.Id,
            EntryType = LedgerEntryType.OrderCancelled,
            QuantityDelta = order.RemainingQuantity,
            CreatedAt = now
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<MarketDepthResponse> GetMarketDepthAsync(Guid assetTypeId, CancellationToken ct = default)
    {
        var assetType = await db.AssetTypes.FindAsync([assetTypeId], ct)
            ?? throw new NotFoundException(nameof(AssetType), assetTypeId);

        var levels = await db.SellOrders
            .Where(o => o.AssetTypeId == assetTypeId &&
                        (o.Status == SellOrderStatus.Open || o.Status == SellOrderStatus.PartiallyFilled))
            .GroupBy(o => o.UnitPrice)
            .OrderBy(g => g.Key)
            .Select(g => new MarketDepthLevelResponse(g.Key, g.Sum(o => o.RemainingQuantity), g.Count()))
            .ToListAsync(ct);

        var bestAsk = levels.Count > 0 ? levels[0].UnitPrice : (decimal?)null;
        var totalVolume = levels.Sum(l => l.TotalQuantity);

        return new MarketDepthResponse(assetTypeId, assetType.Slug, assetType.Name, bestAsk, totalVolume, levels);
    }

    public async Task<decimal?> GetBestAskAsync(Guid assetTypeId, CancellationToken ct = default)
    {
        return await db.SellOrders
            .Where(o => o.AssetTypeId == assetTypeId &&
                        (o.Status == SellOrderStatus.Open || o.Status == SellOrderStatus.PartiallyFilled))
            .MinAsync(o => (decimal?)o.UnitPrice, ct);
    }

    private static SellOrderResponse MapToResponse(SellOrder o) =>
        new(o.Id, o.TraderId, o.Trader?.Name ?? string.Empty,
            o.AssetTypeId, o.AssetType?.Slug ?? string.Empty, o.AssetType?.Name ?? string.Empty,
            o.OriginalQuantity, o.RemainingQuantity, o.FilledQuantity,
            o.UnitPrice, o.Status, o.CreatedAt, o.UpdatedAt);
}
