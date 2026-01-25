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

        b.Property(x => x.ProcessedText)
            .HasColumnType("text")
            .IsRequired();

        b.Property(x => x.Hashtags)
            .HasColumnType("text[]");

        b.Property(x => x.PublishedTo)
            .HasColumnType("integer[]");
    }
}
