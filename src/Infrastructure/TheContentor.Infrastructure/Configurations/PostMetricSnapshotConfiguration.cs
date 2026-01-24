using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class PostMetricSnapshotConfiguration : IEntityTypeConfiguration<PostMetricSnapshot>
{
    public void Configure(EntityTypeBuilder<PostMetricSnapshot> b)
    {
        b.ToTable("PostMetricSnapshots");

        b.HasKey(x => x.Id);

        b.Property(x => x.SourcePostId).IsRequired();
        b.Property(x => x.CapturedUtc).IsRequired();

        b.Property(x => x.Score).IsRequired();
        b.Property(x => x.CommentCount).IsRequired();
        b.Property(x => x.UpvoteRatio);

        b.HasOne(x => x.SourcePost)
            .WithMany(x => x.MetricSnapshots)
            .HasForeignKey(x => x.SourcePostId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate snapshots at the same instant
        b.HasIndex(x => new { x.SourcePostId, x.CapturedUtc }).IsUnique();

        // Fast "latest snapshot"
        b.HasIndex(x => new { x.SourcePostId, x.CapturedUtc });
    }
}