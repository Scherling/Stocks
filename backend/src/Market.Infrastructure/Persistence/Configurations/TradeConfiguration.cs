using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.RequestedQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(t => t.TotalQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(t => t.TotalCost).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(t => t.AverageUnitPrice).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(t => t.ExecutedAt).IsRequired();
        builder.Property(t => t.IdempotencyKey).HasMaxLength(128);

        builder.HasIndex(t => new { t.BuyerTraderId, t.ExecutedAt })
            .HasDatabaseName("ix_trades_buyer_executed");

        builder.HasIndex(t => t.AssetTypeId)
            .HasDatabaseName("ix_trades_asset_type_id");

        // Unique partial index — only for non-null idempotency keys
        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL")
            .HasDatabaseName("ix_trades_idempotency_key");
    }
}
