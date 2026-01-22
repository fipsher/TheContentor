using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class BackgroundAssetConfiguration : IEntityTypeConfiguration<BackgroundAsset>
{
    public void Configure(EntityTypeBuilder<BackgroundAsset> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.LocalPath)
            .IsRequired()
            .HasMaxLength(500);
    }
}
