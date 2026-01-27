using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class ProcessedPostConfiguration : IEntityTypeConfiguration<ProcessedPost>
{
    public void Configure(EntityTypeBuilder<ProcessedPost> b)
    {
        b.ToTable("ProcessedPosts");

        b.HasKey(x => x.Id);

        b.HasOne(x => x.SourcePost)
            .WithOne(x => x.ProcessedPost)
            .HasForeignKey<ProcessedPost>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.Property(x => x.Title)
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.Description)
            .HasColumnType("text")
            .IsRequired();

        b.Property(x => x.Hashtags)
            .HasColumnType("text[]");

        b.Property(x => x.TtsSettings)
            .HasColumnType("text");

        b.OwnsOne(x => x.DescriptionAudioBlobPath, bp =>
        {
            bp.Property(p => p.ContainerName).HasColumnName("DescriptionAudioContainer");
            bp.Property(p => p.AssetPath).HasColumnName("DescriptionAudioPath");
        });
    }
}
