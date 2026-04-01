using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;
using Market.Domain.Enums;

namespace Stocks.Api.Endpoints;

public static class SellOrdersEndpoints
{
    public static void MapSellOrders(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sell-orders").WithTags("Sell Orders");

        group.MapGet("/", async (
            ISellOrderService svc,
            Guid? assetTypeId = null,
            Guid? traderId = null,
            SellOrderStatus? status = null,
            int page = 1, int pageSize = 50,
            CancellationToken ct = default) =>
        {
            pageSize = Math.Clamp(pageSize, 1, 200);
            var result = await svc.ListAsync(assetTypeId, traderId, status, page, pageSize, ct);
            return Results.Ok(result);
        }).WithName("ListSellOrders")
          .WithSummary("List sell orders with optional filters")
          .Produces<PaginatedResponse<SellOrderResponse>>();

        group.MapPost("/", async (
            CreateSellOrderRequest request,
            ISellOrderService svc,
            CancellationToken ct = default) =>
        {
            var result = await svc.CreateAsync(request, ct);
            return Results.Created($"/api/sell-orders/{result.Id}", result);
        }).WithName("CreateSellOrder")
          .WithSummary("Create a new sell order")
          .Produces<SellOrderResponse>(StatusCodes.Status201Created)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound)
          .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", async (
            Guid id, ISellOrderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetSellOrder")
          .WithSummary("Get sell order by ID")
          .Produces<SellOrderResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}", async (
            Guid id, UpdateSellOrderRequest request,
            ISellOrderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.UpdateAsync(id, request, ct);
            return Results.Ok(result);
        }).WithName("UpdateSellOrder")
          .WithSummary("Update sell order price and/or quantity")
          .Produces<SellOrderResponse>()
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (
            Guid id, ISellOrderService svc, CancellationToken ct = default) =>
        {
            await svc.CancelAsync(id, ct);
            return Results.NoContent();
        }).WithName("CancelSellOrder")
          .WithSummary("Cancel a sell order")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/market-depth/{assetTypeId:guid}", async (
            Guid assetTypeId, ISellOrderService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetMarketDepthAsync(assetTypeId, ct);
            return Results.Ok(result);
        }).WithName("GetMarketDepth")
          .WithSummary("Get market depth by price level for an asset type")
          .Produces<MarketDepthResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/best-ask/{assetTypeId:guid}", async (
            Guid assetTypeId, ISellOrderService svc, CancellationToken ct = default) =>
        {
            var price = await svc.GetBestAskAsync(assetTypeId, ct);
            return Results.Ok(new { AssetTypeId = assetTypeId, BestAsk = price });
        }).WithName("GetBestAsk")
          .WithSummary("Get best ask price for an asset type")
          .Produces<object>()
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
