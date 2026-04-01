namespace Market.Domain.Entities;

public class CreditTransfer
{
    public Guid Id { get; set; }
    public Guid? FromTraderId { get; set; }
    public Guid? ToTraderId { get; set; }
    public decimal Amount { get; set; }
    public Guid? TradeId { get; set; }
    public DateTime CreatedAt { get; set; }
}
