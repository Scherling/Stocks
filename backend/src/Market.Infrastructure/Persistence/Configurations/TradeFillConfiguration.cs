using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class TradeFillConfiguration : IEntityTypeConfiguration<TradeFill>
{
    public void Configure(EntityTypeBuilder<TradeFill> builder)
    {
        builder.ToTable("trade_fills");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Quantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(f => f.UnitPrice).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(f => f.SubTotal).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(f => f.ExecutedAt).IsRequired();

        // Analytics index: VWAP, volume over time range
        builder.HasIndex(f => new { f.AssetTypeId, f.ExecutedAt })
            .HasDatabaseName("ix_trade_fills_asset_executed");

        builder.HasIndex(f => f.TradeId)
            .HasDatabaseName("ix_trade_fills_trade_id");
    }
}
