using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Domain.Enums;

namespace Market.Application.Interfaces;

public interface ISellOrderService
{
    Task<PaginatedResponse<SellOrderResponse>> ListAsync(
        Guid? assetTypeId, Guid? traderId, SellOrderStatus? status,
        int page, int pageSize, CancellationToken ct = default);

    Task<SellOrderResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SellOrderResponse> CreateAsync(CreateSellOrderRequest request, CancellationToken ct = default);
    Task<SellOrderResponse> UpdateAsync(Guid id, UpdateSellOrderRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
    Task<MarketDepthResponse> GetMarketDepthAsync(Guid assetTypeId, CancellationToken ct = default);
    Task<decimal?> GetBestAskAsync(Guid assetTypeId, CancellationToken ct = default);
}
