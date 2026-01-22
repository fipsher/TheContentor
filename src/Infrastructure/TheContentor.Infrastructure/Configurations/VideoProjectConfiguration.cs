using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class VideoProjectConfiguration : IEntityTypeConfiguration<VideoProject>
{
    public void Configure(EntityTypeBuilder<VideoProject> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.SourcePost)
            .WithMany()
            .HasForeignKey("SourcePostId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.BackgroundAsset)
            .WithMany()
            .HasForeignKey("BackgroundAssetId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("SourcePostId");
        builder.HasIndex("BackgroundAssetId");

        builder.Property(e => e.VoiceProfileJson)
            .IsRequired();

        builder.Property(e => e.GeneratedAudioPath)
            .HasMaxLength(500);

        builder.Property(e => e.SubtitleFilePath)
            .HasMaxLength(500);

        builder.Property(e => e.FinalVideoPath)
            .HasMaxLength(500);
            
        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}
