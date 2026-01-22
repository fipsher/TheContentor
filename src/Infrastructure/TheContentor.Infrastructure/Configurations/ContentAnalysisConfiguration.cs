using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class ContentAnalysisConfiguration : IEntityTypeConfiguration<ContentAnalysis>
{
    public void Configure(EntityTypeBuilder<ContentAnalysis> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasOne(e => e.SourcePost)
            .WithMany()
            .HasForeignKey("SourcePostId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Criteria)
            .WithMany()
            .HasForeignKey("CriteriaId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("SourcePostId");
        builder.HasIndex("CriteriaId");

        builder.Property(e => e.Labels)
            .IsRequired();
            
        builder.Property(e => e.AiReasoning)
            .IsRequired();
    }
}
