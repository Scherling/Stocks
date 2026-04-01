using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.EntryType).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.QuantityDelta).HasColumnType("numeric(18,6)");
        builder.Property(e => e.CreditDelta).HasColumnType("numeric(18,4)");
        builder.Property(e => e.Metadata).HasColumnType("text");
        builder.Property(e => e.CreatedAt).IsRequired();

        // Trader balance history queries
        builder.HasIndex(e => new { e.TraderId, e.CreatedAt })
            .HasDatabaseName("ix_ledger_trader_created");

        builder.HasIndex(e => e.SellOrderId)
            .HasFilter("sell_order_id IS NOT NULL")
            .HasDatabaseName("ix_ledger_sell_order_id");

        builder.HasIndex(e => e.TradeId)
            .HasFilter("trade_id IS NOT NULL")
            .HasDatabaseName("ix_ledger_trade_id");
    }
}
