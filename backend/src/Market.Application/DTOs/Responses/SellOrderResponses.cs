using Market.Domain.Enums;

namespace Market.Application.DTOs.Responses;

public record SellOrderResponse(
    Guid Id,
    Guid TraderId,
    string TraderName,
    Guid AssetTypeId,
    string AssetCode,
    string AssetName,
    decimal OriginalQuantity,
    decimal RemainingQuantity,
    decimal FilledQuantity,
    decimal UnitPrice,
    SellOrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record MarketDepthLevelResponse(
    decimal UnitPrice,
    decimal TotalQuantity,
    int OrderCount);

public record MarketDepthResponse(
    Guid AssetTypeId,
    string AssetCode,
    string AssetName,
    decimal? BestAsk,
    decimal TotalOpenVolume,
    IReadOnlyList<MarketDepthLevelResponse> Levels);
