using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class SourceCommentConfiguration : IEntityTypeConfiguration<SourceComment>
{
    public void Configure(EntityTypeBuilder<SourceComment> b)
    {
        b.ToTable("SourceComments");

        b.HasKey(x => x.Id);

        b.Property(x => x.SourcePostId).IsRequired();

        b.Property(x => x.ExternalId)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.ParentExternalId)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.AuthorName)
            .HasMaxLength(64)
            .IsRequired();

        b.Property(x => x.RawText)
            .HasColumnType("text")
            .IsRequired();

        b.Property(x => x.Score).IsRequired();
        b.Property(x => x.CreatedUtc).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();

        b.Property(x => x.MetadataJson)
            .HasColumnType("text")
            .HasDefaultValue("{}")
            .IsRequired();

        // Avoid duplicates per post
        b.HasIndex(x => new { x.SourcePostId, x.ExternalId }).IsUnique();

        // Useful queries: top comments
        b.HasIndex(x => new { x.SourcePostId, x.Score });
        b.HasIndex(x => x.CreatedUtc);
    }
}