using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(e => e.Name)
            .IsUnique();

        builder.OwnsOne(e => e.BlobPath);
    }
}
