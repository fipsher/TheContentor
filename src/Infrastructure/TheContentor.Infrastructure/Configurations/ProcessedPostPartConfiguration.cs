using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class ProcessedPostPartConfiguration : IEntityTypeConfiguration<ProcessedPostPart>
{
    public void Configure(EntityTypeBuilder<ProcessedPostPart> b)
    {
        b.ToTable("ProcessedPostParts");

        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ProcessedPostId, x.Part }).IsUnique();

        b.Property(x => x.ProcessedText)
            .HasColumnType("text")
            .IsRequired();

        b.Property(x => x.Hashtags)
            .HasColumnType("text[]");

        b.Property(x => x.PublishedTo)
            .HasColumnType("integer[]");

        b.OwnsOne(x => x.AudioBlobPath, bp =>
        {
            bp.Property(p => p.ContainerName).HasColumnName("AudioContainer");
            bp.Property(p => p.AssetPath).HasColumnName("AudioPath");
        });

        b.OwnsOne(x => x.VideoBlobPath, bp =>
        {
            bp.Property(p => p.ContainerName).HasColumnName("VideoContainer");
            bp.Property(p => p.AssetPath).HasColumnName("VideoPath");
        });

        b.OwnsOne(x => x.SubtitleBlobPath, bp =>
        {
            bp.Property(p => p.ContainerName).HasColumnName("SubtitleContainer");
            bp.Property(p => p.AssetPath).HasColumnName("SubtitlePath");
        });
    }
}
