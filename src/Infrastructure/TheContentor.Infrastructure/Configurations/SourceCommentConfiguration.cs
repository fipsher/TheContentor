using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class SourceCommentConfiguration : IEntityTypeConfiguration<SourceComment>
{
    public void Configure(EntityTypeBuilder<SourceComment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.SourcePost)
            .WithMany(p => p.Comments)
            .HasForeignKey("SourcePostId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("SourcePostId");

        builder.Property(e => e.Author)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Body)
            .IsRequired();
    }
}
