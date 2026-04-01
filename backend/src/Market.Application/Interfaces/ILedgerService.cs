using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface ILedgerService
{
    Task<PaginatedResponse<LedgerEntryResponse>> GetForTraderAsync(
        Guid traderId, int page, int pageSize, CancellationToken ct = default);

    Task<List<LedgerEntryResponse>> GetForSellOrderAsync(
        Guid sellOrderId, CancellationToken ct = default);

    Task<List<LedgerEntryResponse>> GetForTradeAsync(
        Guid tradeId, CancellationToken ct = default);

    Task<PaginatedResponse<AssetTransferResponse>> GetAssetTransfersAsync(
        Guid? traderId, Guid? assetTypeId, int page, int pageSize, CancellationToken ct = default);

    Task<PaginatedResponse<CreditTransferResponse>> GetCreditTransfersAsync(
        Guid? traderId, int page, int pageSize, CancellationToken ct = default);
}
