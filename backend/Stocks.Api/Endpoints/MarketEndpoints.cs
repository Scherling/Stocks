using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class MarketEndpoints
{
    public static void MapMarket(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/market").WithTags("Market Analytics");

        group.MapGet("/{assetTypeId:guid}/best-ask", async (
            Guid assetTypeId, ISellOrderService svc, CancellationToken ct = default) =>
        {
            var price = await svc.GetBestAskAsync(assetTypeId, ct);
            return Results.Ok(new { AssetTypeId = assetTypeId, BestAsk = price });
        }).WithName("MarketBestAsk")
          .WithSummary("Get current best ask for asset type")
          .Produces<object>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{assetTypeId:guid}/depth", async (
            Guid assetTypeId, ISellOrderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetMarketDepthAsync(assetTypeId, ct);
            return Results.Ok(result);
        }).WithName("MarketDepth")
          .WithSummary("Get market depth by price level")
          .Produces<MarketDepthResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{assetTypeId:guid}/recent-trades", async (
            Guid assetTypeId,
            IMarketAnalyticsService svc,
            int limit = 20,
            CancellationToken ct = default) =>
        {
            var result = await svc.GetRecentTradesAsync(assetTypeId, limit, ct);
            return Results.Ok(result);
        }).WithName("MarketRecentTrades")
          .WithSummary("Get recent trade history for asset type")
          .Produces<List<RecentTradeResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{assetTypeId:guid}/stats", async (
            Guid assetTypeId,
            IMarketAnalyticsService svc,
            DateTime? from = null,
            DateTime? to = null,
            CancellationToken ct = default) =>
        {
            var result = await svc.GetStatsAsync(assetTypeId, from, to, ct);
            return Results.Ok(result);
        }).WithName("MarketStats")
          .WithSummary("Get market stats: avg price, VWAP, volume, best ask")
          .WithDescription("Pass optional from/to date range to scope analytics. Based on completed trade fills.")
          .Produces<MarketStatsResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
