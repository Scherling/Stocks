using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class AssetTypesEndpoints
{
    public static void MapAssetTypes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/asset-types").WithTags("Asset Types");

        group.MapGet("/", async (
            IAssetTypeService svc, CancellationToken ct = default) =>
        {
            var result = await svc.ListAsync(ct);
            return Results.Ok(result);
        }).WithName("ListAssetTypes")
          .WithSummary("List all asset types")
          .Produces<List<AssetTypeResponse>>();

        group.MapPost("/", async (
            CreateAssetTypeRequest request,
            IAssetTypeService svc, CancellationToken ct = default) =>
        {
            var result = await svc.CreateAsync(request, ct);
            return Results.Created($"/api/asset-types/{result.Id}", result);
        }).WithName("CreateAssetType")
          .WithSummary("Create a new asset type")
          .Produces<AssetTypeResponse>(StatusCodes.Status201Created)
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", async (
            Guid id, IAssetTypeService svc, CancellationToken ct = default) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return Results.Ok(result);
        }).WithName("GetAssetType")
          .WithSummary("Get asset type by ID")
          .Produces<AssetTypeResponse>()
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateAssetTypeRequest request,
            IAssetTypeService svc, CancellationToken ct = default) =>
        {
            var result = await svc.UpdateAsync(id, request, ct);
            return Results.Ok(result);
        }).WithName("UpdateAssetType")
          .WithSummary("Update asset type")
          .Produces<AssetTypeResponse>()
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id:guid}/deactivate", async (
            Guid id, IAssetTypeService svc, CancellationToken ct = default) =>
        {
            await svc.DeactivateAsync(id, ct);
            return Results.NoContent();
        }).WithName("DeactivateAssetType")
          .WithSummary("Deactivate an asset type")
          .Produces(StatusCodes.Status204NoContent)
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
