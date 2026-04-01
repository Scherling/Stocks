using Market.Domain.Entities;

namespace Market.Application.Services;

/// <summary>
/// Pure, stateless fill calculation. Used by both QuoteService (read-only) and
/// TradeExecutionService (mutating) to guarantee quote == execution.
/// Input orders must already be sorted by unit_price ASC, created_at ASC.
/// </summary>
public static class FillCalculator
{
    public record FillItem(SellOrder SellOrder, decimal Quantity, decimal UnitPrice, decimal SubTotal);

    public record FillResult(
        bool IsFillable,
        decimal TotalQuantity,
        decimal TotalCost,
        decimal AverageUnitPrice,
        decimal MinUnitPrice,
        decimal MaxUnitPrice,
        decimal AvailableQuantity,
        IReadOnlyList<FillItem> Items);

    public static FillResult Compute(IEnumerable<SellOrder> orderedOrders, decimal requestedQuantity)
    {
        var fills = new List<FillItem>();
        decimal remaining = requestedQuantity;
        decimal totalCost = 0m;
        decimal totalAvailable = 0m;

        foreach (var order in orderedOrders)
        {
            totalAvailable += order.RemainingQuantity;

            if (remaining <= 0m) continue;

            var take = Math.Min(order.RemainingQuantity, remaining);
            var subTotal = take * order.UnitPrice;
            fills.Add(new FillItem(order, take, order.UnitPrice, subTotal));
            remaining -= take;
            totalCost += subTotal;
        }

        bool isFillable = remaining <= 0m;
        decimal totalFilled = requestedQuantity - remaining;

        decimal minPrice = fills.Count > 0 ? fills.Min(f => f.UnitPrice) : 0m;
        decimal maxPrice = fills.Count > 0 ? fills.Max(f => f.UnitPrice) : 0m;
        decimal avgPrice = totalFilled > 0 ? totalCost / totalFilled : 0m;

        return new FillResult(
            IsFillable: isFillable,
            TotalQuantity: totalFilled,
            TotalCost: totalCost,
            AverageUnitPrice: avgPrice,
            MinUnitPrice: minPrice,
            MaxUnitPrice: maxPrice,
            AvailableQuantity: totalAvailable,
            Items: fills);
    }
}
