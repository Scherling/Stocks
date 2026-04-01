using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class CreditTransferConfiguration : IEntityTypeConfiguration<CreditTransfer>
{
    public void Configure(EntityTypeBuilder<CreditTransfer> builder)
    {
        builder.ToTable("credit_transfers");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Amount).HasColumnType("numeric(18,4)").IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.HasIndex(t => t.TradeId)
            .HasFilter("trade_id IS NOT NULL")
            .HasDatabaseName("ix_credit_transfers_trade_id");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("ix_credit_transfers_created");
    }
}
