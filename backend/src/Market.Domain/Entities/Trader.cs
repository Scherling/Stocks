using Market.Domain.Enums;

namespace Market.Domain.Entities;

public class Trader
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TraderStatus Status { get; set; } = TraderStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public TraderCreditBalance? CreditBalance { get; set; }
    public ICollection<TraderAssetBalance> AssetBalances { get; set; } = [];
    public ICollection<SellOrder> SellOrders { get; set; } = [];
}
