using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface ITradeExecutionService
{
    Task<TradeResponse> ExecuteAsync(ExecuteTradeRequest request, CancellationToken ct = default);
    Task<TradeResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PaginatedResponse<TradeResponse>> ListAsync(
        Guid? assetTypeId, Guid? buyerTraderId,
        int page, int pageSize, CancellationToken ct = default);
    Task<List<TradeFillResponse>> GetFillsAsync(Guid tradeId, CancellationToken ct = default);
}
