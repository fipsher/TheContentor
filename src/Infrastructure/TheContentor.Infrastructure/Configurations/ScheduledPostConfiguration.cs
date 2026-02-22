using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

/// <summary>EF Core mapping for <see cref="ScheduledPost"/>.</summary>
public sealed class ScheduledPostConfiguration : IEntityTypeConfiguration<ScheduledPost>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<ScheduledPost> b)
    {
        b.ToTable("ScheduledPosts");

        b.HasKey(x => x.Id);

        b.Property(x => x.ScheduledDate)
            .HasColumnType("date")
            .IsRequired();

        b.Property(x => x.CreatedUtc)
            .IsRequired();

        b.Property(x => x.SourcePostId)
            .IsRequired();

        b.HasIndex(x => x.ScheduledDate);

        b.HasIndex(x => x.SourcePostId)
            .IsUnique();

        b.HasOne(x => x.SourcePost)
            .WithMany()
            .HasForeignKey(x => x.SourcePostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
