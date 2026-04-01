using Market.Application.DTOs.Requests;
using Market.Application.DTOs.Responses;
using Market.Application.Exceptions;
using Market.Application.Interfaces;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Market.Application.Services;

public class QuoteService(IMarketDbContext db) : IQuoteService
{
    public async Task<QuoteResponse> GetQuoteAsync(QuoteRequest request, CancellationToken ct = default)
    {
        if (request.Quantity <= 0)
            throw new ValidationException("Quantity must be greater than zero.");

        var assetType = await db.AssetTypes.FindAsync([request.AssetTypeId], ct)
            ?? throw new NotFoundException("AssetType", request.AssetTypeId);

        // Fetch open orders sorted by price ASC, created_at ASC (same order as trade execution)
        var query = db.SellOrders
            .Where(o => o.AssetTypeId == request.AssetTypeId &&
                        (o.Status == SellOrderStatus.Open || o.Status == SellOrderStatus.PartiallyFilled));

        // Exclude buyer's own orders (anti-wash-trade, same as execution)
        if (request.BuyerTraderId.HasValue)
            query = query.Where(o => o.TraderId != request.BuyerTraderId.Value);

        var orders = await query
            .OrderBy(o => o.UnitPrice)
            .ThenBy(o => o.CreatedAt)
            .ToListAsync(ct);

        var result = FillCalculator.Compute(orders, request.Quantity);

        bool? hasSufficientCredits = null;
        if (request.BuyerTraderId.HasValue && result.IsFillable)
        {
            var credit = await db.TraderCreditBalances
                .FirstOrDefaultAsync(c => c.TraderId == request.BuyerTraderId.Value, ct);
            hasSufficientCredits = credit?.Credits >= result.TotalCost;
        }

        var fillPreview = result.Items
            .Select(f => new QuoteFillPreviewResponse(
                f.SellOrder.Id,
                f.SellOrder.TraderId,
                f.Quantity,
                f.UnitPrice,
                f.SubTotal))
            .ToList();

        return new QuoteResponse(
            AssetTypeId: request.AssetTypeId,
            RequestedQuantity: request.Quantity,
            IsFillable: result.IsFillable,
            TotalCost: result.IsFillable ? result.TotalCost : null,
            AverageUnitPrice: result.IsFillable ? result.AverageUnitPrice : null,
            MinUnitPrice: result.IsFillable ? result.MinUnitPrice : null,
            MaxUnitPrice: result.IsFillable ? result.MaxUnitPrice : null,
            OrdersConsumed: result.Items.Count,
            AvailableQuantity: result.AvailableQuantity,
            BuyerHasSufficientCredits: hasSufficientCredits,
            FillPreview: fillPreview);
    }
}
