using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;

namespace Market.Application.Interfaces;

public interface IAssetTypeService
{
    Task<List<AssetTypeResponse>> ListAsync(CancellationToken ct = default);
    Task<AssetTypeResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<AssetTypeResponse> CreateAsync(CreateAssetTypeRequest request, CancellationToken ct = default);
    Task<AssetTypeResponse> UpdateAsync(Guid id, UpdateAssetTypeRequest request, CancellationToken ct = default);
    Task DeactivateAsync(Guid id, CancellationToken ct = default);
}
