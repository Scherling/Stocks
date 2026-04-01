namespace Market.Application.DTOs.Requests;

public record CreateTraderRequest(string Name);

public record UpdateTraderRequest(string Name);

public record AdjustCreditsRequest(decimal Amount, string? Reason);

public record AdjustAssetBalanceRequest(Guid AssetTypeId, decimal Amount, string? Reason);
