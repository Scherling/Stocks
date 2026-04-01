using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class TradersEndpoints
{
    public static void MapTraders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/traders").WithTags("Traders");

        group.MapGet("/", async (
            ITraderService svc,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.ListAsync(page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("ListTraders")
          .WithSummary("List all traders")
          .Produces<PaginatedResponse<TraderResponse>>();

        group.MapPost("/", async (
            CreateTraderRequest request,
            ITraderService svc,
            CancellationToken ct = default) =>
        {
            var result = await svc.CreateAsync(request, ct);
            return Results.Created($"/api/traders/{result.Id}", result);
        }).WithName("CreateTrader")
          .WithSummary("Create a new trader")
          .Produces<TraderResponse>(StatusCodes.Status201Created)
          .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", async (
            Guid id, ITraderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetTrader")
          .WithSummary("Get trader by ID")
          .Produces<TraderResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateTraderRequest request,
            ITraderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.UpdateAsync(id, request, ct);
            return Results.Ok(result);
        }).WithName("UpdateTrader")
          .WithSummary("Update trader")
          .Produces<TraderResponse>()
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id, ITraderService svc, CancellationToken ct = default) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).WithName("DeleteTrader")
          .WithSummary("Soft-delete trader")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/balances", async (
            Guid id, ITraderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetBalancesAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetTraderBalances")
          .WithSummary("Get trader balances (credits + assets)")
          .Produces<TraderBalancesResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/credits/adjust", async (
            Guid id, AdjustCreditsRequest request,
            ITraderService svc, CancellationToken ct = default) =>
        {
            await svc.AdjustCreditsAsync(id, request, ct);
            return Results.NoContent();
        }).WithName("AdjustTraderCredits")
          .WithSummary("Admin: adjust trader credits")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/assets/adjust", async (
            Guid id, AdjustAssetBalanceRequest request,
            ITraderService svc, CancellationToken ct = default) =>
        {
            await svc.AdjustAssetBalanceAsync(id, request, ct);
            return Results.NoContent();
        }).WithName("AdjustTraderAssetBalance")
          .WithSummary("Admin: adjust trader asset balance")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/sell-orders", async (
            Guid id,
            ISellOrderService sellOrderSvc,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await sellOrderSvc.ListAsync(null, id, null, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetTraderSellOrders")
          .WithSummary("Get trader's sell orders")
          .Produces<PaginatedResponse<SellOrderResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/trades", async (
            Guid id,
            ITradeExecutionService tradeSvc,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await tradeSvc.ListAsync(null, id, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetTraderTrades")
          .WithSummary("Get trader's trade history")
          .Produces<PaginatedResponse<TradeResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/ledger", async (
            Guid id,
            ILedgerService ledgerSvc,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await ledgerSvc.GetForTraderAsync(id, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("GetTraderLedger")
          .WithSummary("Get trader's ledger entries")
          .Produces<PaginatedResponse<LedgerEntryResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
