using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public sealed class RedditPostDataConfiguration : IEntityTypeConfiguration<RedditPostData>
{
    public void Configure(EntityTypeBuilder<RedditPostData> b)
    {
        b.ToTable("RedditPostData");
        b.HasKey(x => x.Id);

        b.Property(x => x.Subreddit)
            .HasMaxLength(128)
            .IsRequired();

        b.Property(x => x.Permalink)
            .HasMaxLength(512)
            .IsRequired();

        b.Property(x => x.FullName) // t3_xxx
            .HasMaxLength(32)
            .IsRequired();

        b.Property(x => x.IsSelfPost).IsRequired();

        b.Property(x => x.LinkUrl)
            .HasMaxLength(1024);

        b.Property(x => x.Domain)
            .HasMaxLength(128);

        b.Property(x => x.FlairText)
            .HasMaxLength(256);

        b.Property(x => x.IsAuthorDeleted).IsRequired();

        b.Property(x => x.AuthorCreatedUtc);
        b.Property(x => x.AuthorLinkKarma);
        b.Property(x => x.AuthorCommentKarma);

        b.Property(x => x.IsLocked).IsRequired();
        b.Property(x => x.IsRemoved).IsRequired();
        b.Property(x => x.IsDeleted).IsRequired();
        b.Property(x => x.IsStickied).IsRequired();
        b.Property(x => x.IsArchived).IsRequired();

        b.Property(x => x.TotalAwardsReceived);

        // Relationship defined on SourcePost side too, but safe to declare here as well:
        b.HasOne(x => x.SourcePost)
            .WithOne()
            .HasForeignKey<RedditPostData>(x => x.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.FullName).IsUnique();         // optional, but useful
        b.HasIndex(x => x.Subreddit);
    }
}