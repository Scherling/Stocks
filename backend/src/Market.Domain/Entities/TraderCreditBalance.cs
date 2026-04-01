namespace Market.Domain.Entities;

public class TraderCreditBalance
{
    public Guid Id { get; set; }
    public Guid TraderId { get; set; }
    public decimal Credits { get; set; }

    public Trader? Trader { get; set; }
}
