using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface IMarketAnalyticsService
{
    Task<MarketStatsResponse> GetStatsAsync(
        Guid assetTypeId, DateTime? from, DateTime? to, CancellationToken ct = default);

    Task<List<RecentTradeResponse>> GetRecentTradesAsync(
        Guid assetTypeId, int limit, CancellationToken ct = default);
}
