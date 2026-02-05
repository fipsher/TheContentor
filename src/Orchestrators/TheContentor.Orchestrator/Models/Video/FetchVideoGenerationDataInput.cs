namespace TheContentor.Orchestrator.Models.Video;

public class FetchVideoGenerationDataInput
{
    public Guid ProcessedPostId { get; set; }
    public List<Guid> AssetIds { get; set; } = [];
}
