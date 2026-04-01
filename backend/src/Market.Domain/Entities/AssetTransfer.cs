namespace Market.Domain.Entities;

public class AssetTransfer
{
    public Guid Id { get; set; }
    public Guid? FromTraderId { get; set; }
    public Guid? ToTraderId { get; set; }
    public Guid AssetTypeId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? TradeId { get; set; }
    public Guid? SellOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
}
