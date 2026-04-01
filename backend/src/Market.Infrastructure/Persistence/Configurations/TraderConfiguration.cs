using Market.Domain.Entities;
using Market.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class TraderConfiguration : IEntityTypeConfiguration<Trader>
{
    public void Configure(EntityTypeBuilder<Trader> builder)
    {
        builder.ToTable("traders");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();
        builder.HasIndex(t => t.Name).HasDatabaseName("ix_traders_name");
    }
}
