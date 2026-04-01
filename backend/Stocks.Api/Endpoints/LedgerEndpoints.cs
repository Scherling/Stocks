using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class LedgerEndpoints
{
    public static void MapLedger(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ledger").WithTags("Ledger & Audit");

        group.MapGet("/trader/{id:guid}", async (
            Guid id, ILedgerService svc,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.GetForTraderAsync(id, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetLedgerForTrader")
          .WithSummary("Get ledger entries for a trader")
          .Produces<PaginatedResponse<LedgerEntryResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/sell-order/{id:guid}", async (
            Guid id, ILedgerService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetForSellOrderAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetLedgerForSellOrder")
          .WithSummary("Get ledger entries for a sell order")
          .Produces<List<LedgerEntryResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/trade/{id:guid}", async (
            Guid id, ILedgerService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetForTradeAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetLedgerForTrade")
          .WithSummary("Get ledger entries for a trade")
          .Produces<List<LedgerEntryResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/asset-transfers", async (
            ILedgerService svc,
            Guid? traderId = null,
            Guid? assetTypeId = null,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.GetAssetTransfersAsync(traderId, assetTypeId, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetAssetTransfers")
          .WithSummary("Get asset transfer history")
          .Produces<PaginatedResponse<AssetTransferResponse>>();

        group.MapGet("/credit-transfers", async (
            ILedgerService svc,
            Guid? traderId = null,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.GetCreditTransfersAsync(traderId, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetCreditTransfers")
          .WithSummary("Get credit transfer history")
          .Produces<PaginatedResponse<CreditTransferResponse>>();
    }
}
