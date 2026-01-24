using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class SourcePostConfiguration : IEntityTypeConfiguration<SourcePost>
{
    public void Configure(EntityTypeBuilder<SourcePost> b)
    {
        b.ToTable("SourcePosts");

        b.HasKey(x => x.Id);

        // Core identity
        b.Property(x => x.Platform)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.ExternalId)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.ExternalUrl)
            .HasMaxLength(512)
            .IsRequired();

        // Origin
        b.Property(x => x.Community)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.CommunityExternalId)
            .HasMaxLength(64);

        b.Property(x => x.Flairs)
            .HasMaxLength(512);

        // Author (minimal)
        b.Property(x => x.AuthorExternalId)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.AuthorName)
            .HasMaxLength(64)
            .IsRequired();

        // Content
        b.Property(x => x.Title)
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.RawText)
            .HasColumnType("text")
            .IsRequired();

        b.Property(x => x.RawHtml)
            .HasColumnType("text");

        b.Property(x => x.Language)
            .HasMaxLength(16)
            .IsRequired();

        b.Property(x => x.WordCount)
            .IsRequired();

        // Engagement snapshot at ingest time
        b.Property(x => x.Score).IsRequired();
        b.Property(x => x.CommentCount).IsRequired();
        b.Property(x => x.UpvoteRatio);
        b.Property(x => x.IsNsfw).IsRequired();
        b.Property(x => x.IsSpoiler).IsRequired();

        // Timing
        b.Property(x => x.CreatedUtc).IsRequired();
        b.Property(x => x.IngestedUtc).IsRequired();
        b.Property(x => x.LastRefreshedUtc);

        // Processing
        b.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.StatusReason)
            .HasMaxLength(512);

        // Dedup
        b.Property(x => x.ContentHash)
            .HasMaxLength(64)
            .IsRequired();

        // Raw payload
        b.Property(x => x.MetadataJson)
            .HasColumnType("text")
            .HasDefaultValue("{}")
            .IsRequired();

        // Relationships
        b.HasMany(x => x.Comments)
            .WithOne(x => x.SourcePost)
            .HasForeignKey(x => x.SourcePostId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.MetricSnapshots)
            .WithOne(x => x.SourcePost)
            .HasForeignKey(x => x.SourcePostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes / constraints
        b.HasIndex(x => new { x.Platform, x.ExternalId }).IsUnique();

        b.HasIndex(x => new { x.Platform, x.Community, x.CreatedUtc });
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.IngestedUtc);
        b.HasIndex(x => x.ContentHash);

        // Helpful for "what's trending now"
        b.HasIndex(x => new { x.Platform, x.Community, x.Score, x.CreatedUtc });
    }
}