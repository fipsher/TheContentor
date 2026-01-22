using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Configurations;

public class AnalysisCriteriaConfiguration : IEntityTypeConfiguration<AnalysisCriteria>
{
    public void Configure(EntityTypeBuilder<AnalysisCriteria> builder)
    {
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.SystemPrompt)
            .IsRequired();
    }
}
