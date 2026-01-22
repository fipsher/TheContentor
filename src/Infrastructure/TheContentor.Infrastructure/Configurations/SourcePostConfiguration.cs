using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class SourcePostConfiguration : IEntityTypeConfiguration<SourcePost>
{
    public void Configure(EntityTypeBuilder<SourcePost> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.RawText)
            .IsRequired();

        builder.HasMany(e => e.Comments)
            .WithOne(e => e.SourcePost)
            .HasForeignKey("SourcePostId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
