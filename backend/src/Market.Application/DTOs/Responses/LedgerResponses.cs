using Market.Domain.Enums;

namespace Market.Application.DTOs.Responses;

public record LedgerEntryResponse(
    Guid Id,
    Guid TraderId,
    Guid? AssetTypeId,
    string? AssetCode,
    Guid? TradeId,
    Guid? SellOrderId,
    LedgerEntryType EntryType,
    string EntryTypeName,
    decimal? QuantityDelta,
    decimal? CreditDelta,
    string? Metadata,
    DateTime CreatedAt);

public record AssetTransferResponse(
    Guid Id,
    Guid? FromTraderId,
    string? FromTraderName,
    Guid? ToTraderId,
    string? ToTraderName,
    Guid AssetTypeId,
    string AssetCode,
    decimal Quantity,
    Guid? TradeId,
    DateTime CreatedAt);

public record CreditTransferResponse(
    Guid Id,
    Guid? FromTraderId,
    string? FromTraderName,
    Guid? ToTraderId,
    string? ToTraderName,
    decimal Amount,
    Guid? TradeId,
    DateTime CreatedAt);
