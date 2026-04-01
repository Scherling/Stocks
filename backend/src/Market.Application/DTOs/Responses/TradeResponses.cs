namespace Market.Application.DTOs.Responses;

public record QuoteFillPreviewResponse(
    Guid SellOrderId,
    Guid SellerTraderId,
    decimal Quantity,
    decimal UnitPrice,
    decimal SubTotal);

public record QuoteResponse(
    Guid AssetTypeId,
    decimal RequestedQuantity,
    bool IsFillable,
    decimal? TotalCost,
    decimal? AverageUnitPrice,
    decimal? MinUnitPrice,
    decimal? MaxUnitPrice,
    int OrdersConsumed,
    decimal AvailableQuantity,
    bool? BuyerHasSufficientCredits,
    IReadOnlyList<QuoteFillPreviewResponse> FillPreview);

public record TradeFillResponse(
    Guid Id,
    Guid SellOrderId,
    Guid SellerTraderId,
    decimal Quantity,
    decimal UnitPrice,
    decimal SubTotal,
    DateTime ExecutedAt);

public record TradeResponse(
    Guid Id,
    Guid BuyerTraderId,
    Guid AssetTypeId,
    string AssetCode,
    decimal RequestedQuantity,
    decimal TotalQuantity,
    decimal TotalCost,
    decimal AverageUnitPrice,
    DateTime ExecutedAt,
    string? IdempotencyKey,
    IReadOnlyList<TradeFillResponse> Fills);
