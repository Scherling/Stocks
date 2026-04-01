namespace Market.Application.DTOs.Responses;

public record MarketStatsResponse(
    Guid AssetTypeId,
    string AssetCode,
    DateTime? From,
    DateTime? To,
    decimal? LatestTradedPrice,
    decimal? AveragePrice,
    decimal? Vwap,
    decimal TotalVolume,
    long TotalTradeCount,
    decimal? BestAsk,
    decimal OpenSellVolume);

public record RecentTradeResponse(
    Guid TradeId,
    Guid BuyerTraderId,
    decimal TotalQuantity,
    decimal AverageUnitPrice,
    decimal TotalCost,
    DateTime ExecutedAt);
