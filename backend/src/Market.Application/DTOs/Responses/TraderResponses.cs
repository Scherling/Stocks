using Market.Domain.Enums;

namespace Market.Application.DTOs.Responses;

public record TraderResponse(
    Guid Id,
    string Name,
    TraderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record TraderBalancesResponse(
    Guid TraderId,
    decimal Credits,
    IReadOnlyList<AssetBalanceResponse> AssetBalances);

public record AssetBalanceResponse(
    Guid AssetTypeId,
    string AssetCode,
    string AssetName,
    decimal TotalQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);
