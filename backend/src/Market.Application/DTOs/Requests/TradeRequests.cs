namespace Market.Application.DTOs.Requests;

public record ExecuteTradeRequest(
    Guid BuyerTraderId,
    Guid AssetTypeId,
    decimal Quantity,
    string? IdempotencyKey = null);

public record QuoteRequest(
    Guid AssetTypeId,
    decimal Quantity,
    Guid? BuyerTraderId = null);
