namespace Market.Domain.Entities;

public class Trade
{
    public Guid Id { get; set; }
    public Guid BuyerTraderId { get; set; }
    public Guid AssetTypeId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AverageUnitPrice { get; set; }
    public DateTime ExecutedAt { get; set; }

    // Optional idempotency key — unique when non-null
    public string? IdempotencyKey { get; set; }

    public Trader? BuyerTrader { get; set; }
    public AssetType? AssetType { get; set; }
    public ICollection<TradeFill> Fills { get; set; } = [];
}
