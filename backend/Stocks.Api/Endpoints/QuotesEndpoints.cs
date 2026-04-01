using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Interfaces;

namespace Stocks.Api.Endpoints;

public static class QuotesEndpoints
{
    public static void MapQuotes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes").WithTags("Quotes");

        // GET /api/quotes?assetTypeId=...&quantity=...&buyerTraderId=...
        group.MapGet("/", async (
            IQuoteService svc,
            Guid assetTypeId,
            decimal quantity,
            Guid? buyerTraderId = null,
            CancellationToken ct = default) =>
        {
            var request = new QuoteRequest(assetTypeId, quantity, buyerTraderId);
            var result = await svc.GetQuoteAsync(request, ct);
            return Results.Ok(result);
        }).WithName("GetQuote")
          .WithSummary("Get a purchase quote (read-only — does not mutate state)")
          .WithDescription("Returns whether the requested quantity can be filled, total cost, VWAP, and a detailed fill preview. Pass buyerTraderId to also check credit sufficiency.")
          .Produces<QuoteResponse>()
          .ProducesProblem(StatusCodes.Status400BadRequest)
          .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
