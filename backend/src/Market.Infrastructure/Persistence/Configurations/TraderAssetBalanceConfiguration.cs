using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class TraderAssetBalanceConfiguration : IEntityTypeConfiguration<TraderAssetBalance>
{
    public void Configure(EntityTypeBuilder<TraderAssetBalance> builder)
    {
        builder.ToTable("trader_asset_balances");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.TotalQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(b => b.ReservedQuantity).HasColumnType("numeric(18,6)").IsRequired();
        builder.Ignore(b => b.AvailableQuantity); // Computed, not persisted
        // NOTE: Concurrency is handled via pessimistic FOR UPDATE locks in TradeExecutionService.
        // xmin concurrency token can be added here in future: builder.UseXminAsConcurrencyToken()

        builder.HasIndex(b => new { b.TraderId, b.AssetTypeId })
            .IsUnique()
            .HasDatabaseName("ix_trader_asset_balances_trader_asset");
    }
}
