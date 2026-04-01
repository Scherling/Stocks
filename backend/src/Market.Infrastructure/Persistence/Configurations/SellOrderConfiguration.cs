using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class SellOrderConfiguration : IEntityTypeConfiguration<SellOrder>
{
    public void Configure(EntityTypeBuilder<SellOrder> builder)
    {
        builder.ToTable("sell_orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.OriginalQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.RemainingQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.UnitPrice).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();
        builder.Ignore(o => o.FilledQuantity); // Computed, not persisted
        // Concurrency handled via pessimistic FOR UPDATE locks. xmin token can be added here in future.

        // Primary index for fill-finding queries: filter by asset+status, order by price+time
        builder.HasIndex(o => new { o.AssetTypeId, o.Status, o.UnitPrice, o.CreatedAt })
            .HasDatabaseName("ix_sell_orders_asset_status_price_created");

        builder.HasIndex(o => o.TraderId)
            .HasDatabaseName("ix_sell_orders_trader_id");
    }
}
