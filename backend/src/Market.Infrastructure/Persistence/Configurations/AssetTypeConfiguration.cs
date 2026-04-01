using Market.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Market.Infrastructure.Persistence.Configurations;

public class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder)
    {
        builder.ToTable("asset_types");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Slug).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.Property(a => a.UnitName).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Category).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Stage).HasMaxLength(50).IsRequired();
        builder.Property(a => a.Description).HasMaxLength(2000);
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
        builder.HasIndex(a => a.Slug).IsUnique().HasDatabaseName("ix_asset_types_code");
    }
}
