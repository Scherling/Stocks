using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class TradesEndpoints
{
    public static void MapTrades(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/trades").WithTags("Trades");

        group.MapPost("/", async (
            ExecuteTradeRequest request,
            ITradeExecutionService svc,
            HttpContext http,
            CancellationToken ct = default) =>
        {
            // Accept idempotency key from either header or request body
            var idempotencyKey = http.Request.Headers["Idempotency-Key"].FirstOrDefault()
                ?? request.IdempotencyKey;
            var fullRequest = request with { IdempotencyKey = idempotencyKey };
            var result = await svc.ExecuteAsync(fullRequest, ct);
            return Results.Ok(result);
        }).WithName("ExecuteTrade")
          .WithSummary("Execute a purchase")
          .WithDescription("Purchases the requested quantity at the best available prices. Supply Idempotency-Key header to safely retry. Fails if full quantity cannot be filled.")
          .Produces<TradeResponse>()
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/", async (
            ITradeExecutionService svc,
            Guid? assetTypeId = null,
            Guid? buyerTraderId = null,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.ListAsync(assetTypeId, buyerTraderId, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("ListTrades")
          .WithSummary("List trades with optional filters")
          .Produces<PaginatedResponse<TradeResponse>>();

        group.MapGet("/{id:guid}", async (
            Guid id, ITradeExecutionService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetTrade")
          .WithSummary("Get trade by ID")
          .Produces<TradeResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/fills", async (
            Guid id, ITradeExecutionService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetFillsAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetTradeFills")
          .WithSummary("Get fills for a trade")
          .Produces<List<TradeFillResponse>>()
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
