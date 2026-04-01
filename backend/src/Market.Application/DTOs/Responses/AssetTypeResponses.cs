namespace Market.Application.DTOs.Responses;

public record AssetTypeResponse(
    Guid Id,
    string Slug,
    string Name,
    string UnitName,
    string Category,
    string Stage,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);
