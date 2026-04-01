using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface ITraderService
{
    Task<PaginatedResponse<TraderResponse>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<TraderResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TraderResponse> CreateAsync(CreateTraderRequest request, CancellationToken ct = default);
    Task<TraderResponse> UpdateAsync(Guid id, UpdateTraderRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<TraderBalancesResponse> GetBalancesAsync(Guid id, CancellationToken ct = default);
    Task AdjustCreditsAsync(Guid id, AdjustCreditsRequest request, CancellationToken ct = default);
    Task AdjustAssetBalanceAsync(Guid id, AdjustAssetBalanceRequest request, CancellationToken ct = default);
}
