using Market.Domain.Enums;

namespace Market.Domain.Entities;

public class SellOrder
{
    public Guid Id { get; set; }
    public Guid TraderId { get; set; }
    public Guid AssetTypeId { get; set; }
    public decimal OriginalQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public SellOrderStatus Status { get; set; } = SellOrderStatus.Open;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Computed — not persisted
    public decimal FilledQuantity => OriginalQuantity - RemainingQuantity;

    public Trader? Trader { get; set; }
    public AssetType? AssetType { get; set; }
    public ICollection<TradeFill> Fills { get; set; } = [];
}
