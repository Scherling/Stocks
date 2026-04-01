namespace Market.Application.DTOs.Requests;

public record CreateSellOrderRequest(
    Guid TraderId,
    Guid AssetTypeId,
    decimal Quantity,
    decimal UnitPrice);

public record UpdateSellOrderRequest(
    decimal? UnitPrice,
    decimal? Quantity);
