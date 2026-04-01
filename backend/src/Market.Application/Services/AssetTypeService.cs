using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class AssetTypeService(IMarketDbContext db) : IAssetTypeService
{
    public async Task<List<AssetTypeResponse>> ListAsync(CancellationToken ct = default)
    {
        return await db.AssetTypes
            .OrderBy(a => a.Slug)
            .Select(a => MapToResponse(a))
            .ToListAsync(ct);
    }

    public async Task<AssetTypeResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.AssetTypes.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(AssetType), id);
        return MapToResponse(asset);
    }

    public async Task<AssetTypeResponse> CreateAsync(CreateAssetTypeRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Slug))
            throw new ValidationException("Asset ID cannot be empty.");
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Asset name cannot be empty.");

        var slug = request.Slug.Trim().ToLowerInvariant();
        var existing = await db.AssetTypes.AnyAsync(a => a.Slug == slug, ct);
        if (existing)
            throw new ConflictException($"Asset type with ID '{slug}' already exists.");

        var now = DateTime.UtcNow;
        var asset = new AssetType
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = request.Name.Trim(),
            UnitName = request.UnitName.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AssetTypes.Add(asset);
        await db.SaveChangesAsync(ct);

        return MapToResponse(asset);
    }

    public async Task<AssetTypeResponse> UpdateAsync(Guid id, UpdateAssetTypeRequest request, CancellationToken ct = default)
    {
        var asset = await db.AssetTypes.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(AssetType), id);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Asset name cannot be empty.");

        asset.Name = request.Name.Trim();
        asset.UnitName = request.UnitName.Trim();
        asset.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return MapToResponse(asset);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var asset = await db.AssetTypes.FindAsync([id], ct)
            ?? throw new NotFoundException(nameof(AssetType), id);

        asset.IsActive = false;
        asset.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static AssetTypeResponse MapToResponse(AssetType a) =>
        new(a.Id, a.Slug, a.Name, a.UnitName, a.Category, a.Stage, a.Description, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
