namespace Market.Domain.Entities;

public class TradeFill
{
    public Guid Id { get; set; }
    public Guid TradeId { get; set; }
    public Guid SellOrderId { get; set; }
    public Guid SellerTraderId { get; set; }
    public Guid AssetTypeId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public DateTime ExecutedAt { get; set; }

    public Trade? Trade { get; set; }
    public SellOrder? SellOrder { get; set; }
}
