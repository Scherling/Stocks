using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class AssetTransferConfiguration : IEntityTypeConfiguration<AssetTransfer>
{
    public void Configure(EntityTypeBuilder<AssetTransfer> builder)
    {
        builder.ToTable("asset_transfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Quantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.TradeId)
            .HasFilter("trade_id IS NOT NULL")
            .HasDatabaseName("ix_asset_transfers_trade_id");

        builder.HasIndex(t => new { t.AssetTypeId, t.CreatedAt })
            .HasDatabaseName("ix_asset_transfers_asset_created");
    }
}
