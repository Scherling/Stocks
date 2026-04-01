using Market.Domain.Enums;

namespace Market.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid TraderId { get; set; }
    public Guid? AssetTypeId { get; set; }
    public Guid? TradeId { get; set; }
    public Guid? SellOrderId { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public decimal? QuantityDelta { get; set; }
    public decimal? CreditDelta { get; set; }

    // Optional JSON metadata for additional context
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}
