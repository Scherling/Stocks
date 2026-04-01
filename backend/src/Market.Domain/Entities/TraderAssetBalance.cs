namespace Market.Domain.Entities;

public class TraderAssetBalance
{
    public Guid Id { get; set; }
    public Guid TraderId { get; set; }
    public Guid AssetTypeId { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }

    // Computed — not persisted
    public decimal AvailableQuantity => TotalQuantity - ReservedQuantity;

    public Trader? Trader { get; set; }
    public AssetType? AssetType { get; set; }
}
