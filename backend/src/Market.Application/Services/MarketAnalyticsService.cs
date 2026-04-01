using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class MarketAnalyticsService(IMarketDbContext db) : IMarketAnalyticsService
{
    public async Task<MarketStatsResponse> GetStatsAsync(
        Guid assetTypeId, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var assetType = await db.AssetTypes.FindAsync([assetTypeId], ct)
            ?? throw new NotFoundException(nameof(AssetType), assetTypeId);

        // Trade fills are the source of truth for analytics
        var fillsQuery = db.TradeFills.Where(f => f.AssetTypeId == assetTypeId);
        if (from.HasValue) fillsQuery = fillsQuery.Where(f => f.ExecutedAt >= from.Value);
        if (to.HasValue) fillsQuery = fillsQuery.Where(f => f.ExecutedAt <= to.Value);

        var fillStats = await fillsQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalVolume = g.Sum(f => f.Quantity),
                TotalCost = g.Sum(f => f.SubTotal),
                TradeCount = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var latestPrice = await db.TradeFills
            .Where(f => f.AssetTypeId == assetTypeId)
            .OrderByDescending(f => f.ExecutedAt)
            .Select(f => (decimal?)f.UnitPrice)
            .FirstOrDefaultAsync(ct);

        decimal? avgPrice = fillStats?.TotalVolume > 0
            ? fillStats.TotalCost / fillStats.TotalVolume
            : null;

        // VWAP = sum(qty * price) / sum(qty)
        decimal? vwap = fillStats?.TotalVolume > 0
            ? fillStats.TotalCost / fillStats.TotalVolume
            : null;

        // Open sell volume
        var openVolume = await db.SellOrders
            .Where(o => o.AssetTypeId == assetTypeId &&
                        (o.Status == SellOrderStatus.Open || o.Status == SellOrderStatus.PartiallyFilled))
            .SumAsync(o => (decimal?)o.RemainingQuantity, ct) ?? 0m;

        // Best ask
        var bestAsk = await db.SellOrders
            .Where(o => o.AssetTypeId == assetTypeId &&
                        (o.Status == SellOrderStatus.Open || o.Status == SellOrderStatus.PartiallyFilled))
            .MinAsync(o => (decimal?)o.UnitPrice, ct);

        return new MarketStatsResponse(
            AssetTypeId: assetTypeId,
            AssetCode: assetType.Slug,
            From: from,
            To: to,
            LatestTradedPrice: latestPrice,
            AveragePrice: avgPrice,
            Vwap: vwap,
            TotalVolume: fillStats?.TotalVolume ?? 0m,
            TotalTradeCount: fillStats?.TradeCount ?? 0,
            BestAsk: bestAsk,
            OpenSellVolume: openVolume);
    }

    public async Task<List<RecentTradeResponse>> GetRecentTradesAsync(
        Guid assetTypeId, int limit, CancellationToken ct = default)
    {
        if (!await db.AssetTypes.AnyAsync(a => a.Id == assetTypeId, ct))
            throw new NotFoundException(nameof(AssetType), assetTypeId);

        limit = Math.Clamp(limit, 1, 200);

        return await db.Trades
            .Where(t => t.AssetTypeId == assetTypeId)
            .OrderByDescending(t => t.ExecutedAt)
            .Take(limit)
            .Select(t => new RecentTradeResponse(
                t.Id, t.BuyerTraderId,
                t.TotalQuantity, t.AverageUnitPrice, t.TotalCost,
                t.ExecutedAt))
            .ToListAsync(ct);
    }
}
