using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class LedgerService(IMarketDbContext db) : ILedgerService
{
    public async Task<PaginatedResponse<LedgerEntryResponse>> GetForTraderAsync(
        Guid traderId, int page, int pageSize, CancellationToken ct = default)
    {
        if (!await db.Traders.AnyAsync(t => t.Id == traderId, ct))
            throw new NotFoundException(nameof(Trader), traderId);

        var query = db.LedgerEntries
            .Where(e => e.TraderId == traderId)
            .OrderByDescending(e => e.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var assetTypeIds = items.Where(i => i.AssetTypeId.HasValue).Select(i => i.AssetTypeId!.Value).Distinct().ToList();
        var assetCodes = await db.AssetTypes
            .Where(a => assetTypeIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Slug, ct);

        var responses = items.Select(e => MapToResponse(e, assetCodes)).ToList();
        return new PaginatedResponse<LedgerEntryResponse>(responses, total, page, pageSize);
    }

    public async Task<List<LedgerEntryResponse>> GetForSellOrderAsync(Guid sellOrderId, CancellationToken ct = default)
    {
        if (!await db.SellOrders.AnyAsync(o => o.Id == sellOrderId, ct))
            throw new NotFoundException(nameof(SellOrder), sellOrderId);

        var items = await db.LedgerEntries
            .Where(e => e.SellOrderId == sellOrderId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        return await EnrichAndMap(items, ct);
    }

    public async Task<List<LedgerEntryResponse>> GetForTradeAsync(Guid tradeId, CancellationToken ct = default)
    {
        if (!await db.Trades.AnyAsync(t => t.Id == tradeId, ct))
            throw new NotFoundException(nameof(Trade), tradeId);

        var items = await db.LedgerEntries
            .Where(e => e.TradeId == tradeId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(ct);

        return await EnrichAndMap(items, ct);
    }

    public async Task<PaginatedResponse<AssetTransferResponse>> GetAssetTransfersAsync(
        Guid? traderId, Guid? assetTypeId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AssetTransfers.AsQueryable();
        if (traderId.HasValue)
            query = query.Where(t => t.FromTraderId == traderId || t.ToTraderId == traderId);
        if (assetTypeId.HasValue)
            query = query.Where(t => t.AssetTypeId == assetTypeId);

        query = query.OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var traderIds = items.SelectMany(i => new[] { i.FromTraderId, i.ToTraderId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var assetIds = items.Select(i => i.AssetTypeId).Distinct().ToList();

        var traders = await db.Traders.Where(t => traderIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);
        var assets = await db.AssetTypes.Where(a => assetIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Slug, ct);

        var responses = items.Select(i => new AssetTransferResponse(
            i.Id,
            i.FromTraderId, i.FromTraderId.HasValue ? traders.GetValueOrDefault(i.FromTraderId.Value) : null,
            i.ToTraderId, i.ToTraderId.HasValue ? traders.GetValueOrDefault(i.ToTraderId.Value) : null,
            i.AssetTypeId, assets.GetValueOrDefault(i.AssetTypeId, string.Empty),
            i.Quantity, i.TradeId, i.CreatedAt)).ToList();

        return new PaginatedResponse<AssetTransferResponse>(responses, total, page, pageSize);
    }

    public async Task<PaginatedResponse<CreditTransferResponse>> GetCreditTransfersAsync(
        Guid? traderId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.CreditTransfers.AsQueryable();
        if (traderId.HasValue)
            query = query.Where(t => t.FromTraderId == traderId || t.ToTraderId == traderId);

        query = query.OrderByDescending(t => t.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        var traderIds = items.SelectMany(i => new[] { i.FromTraderId, i.ToTraderId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var traders = await db.Traders.Where(t => traderIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, ct);

        var responses = items.Select(i => new CreditTransferResponse(
            i.Id,
            i.FromTraderId, i.FromTraderId.HasValue ? traders.GetValueOrDefault(i.FromTraderId.Value) : null,
            i.ToTraderId, i.ToTraderId.HasValue ? traders.GetValueOrDefault(i.ToTraderId.Value) : null,
            i.Amount, i.TradeId, i.CreatedAt)).ToList();

        return new PaginatedResponse<CreditTransferResponse>(responses, total, page, pageSize);
    }

    private async Task<List<LedgerEntryResponse>> EnrichAndMap(List<LedgerEntry> items, CancellationToken ct)
    {
        var assetTypeIds = items.Where(i => i.AssetTypeId.HasValue).Select(i => i.AssetTypeId!.Value).Distinct().ToList();
        var assetCodes = await db.AssetTypes
            .Where(a => assetTypeIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Slug, ct);

        return items.Select(e => MapToResponse(e, assetCodes)).ToList();
    }

    private static LedgerEntryResponse MapToResponse(LedgerEntry e, Dictionary<Guid, string> assetCodes) =>
        new(e.Id, e.TraderId,
            e.AssetTypeId, e.AssetTypeId.HasValue ? assetCodes.GetValueOrDefault(e.AssetTypeId.Value) : null,
            e.TradeId, e.SellOrderId,
            e.EntryType, e.EntryType.ToString(),
            e.QuantityDelta, e.CreditDelta,
            e.Metadata, e.CreatedAt);
}
