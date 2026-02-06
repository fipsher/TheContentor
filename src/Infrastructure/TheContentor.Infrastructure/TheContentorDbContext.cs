using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure;

public class TheContentorDbContext(DbContextOptions<TheContentorDbContext> options) : DbContext(options)
{
    public DbSet<AnalysisCriteria> AnalysisCriteria { get; set; } = null!;
    public DbSet<ContentAnalysis> ContentAnalyses { get; set; } = null!;
    public virtual DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<SourcePost> SourcePosts { get; set; } = null!;
    public DbSet<SourceComment> SourceComments { get; set; } = null!;
    public DbSet<RedditPostData> RedditPostData { get; set; } = null!;
    public DbSet<PostMetricSnapshot> PostMetricSnapshots { get; set; } = null!;
    public DbSet<VideoProject> VideoProjects { get; set; } = null!;
    public DbSet<ProcessedPost> ProcessedPosts { get; set; } = null!;
    public DbSet<ProcessedPostPart> ProcessedPostParts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TheContentorDbContext).Assembly);
    }
}
