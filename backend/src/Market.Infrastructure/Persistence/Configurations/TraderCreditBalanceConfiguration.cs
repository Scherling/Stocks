using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class TraderCreditBalanceConfiguration : IEntityTypeConfiguration<TraderCreditBalance>
{
    public void Configure(EntityTypeBuilder<TraderCreditBalance> builder)
    {
        builder.ToTable("trader_credit_balances");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Credits).HasColumnType("numeric(18,4)").IsRequired();
        // Concurrency handled via pessimistic FOR UPDATE locks. xmin token can be added here in future.

        builder.HasIndex(b => b.TraderId)
            .IsUnique()
            .HasDatabaseName("ix_trader_credit_balances_trader_id");
    }
}
