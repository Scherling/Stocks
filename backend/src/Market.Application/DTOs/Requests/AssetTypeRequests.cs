namespace Market.Application.DTOs.Requests;

public record CreateAssetTypeRequest(string Slug, string Name, string UnitName);

public record UpdateAssetTypeRequest(string Name, string UnitName);
