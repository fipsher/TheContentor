namespace TheContentor.Orchestrator.Models.ProcessedPost;

public class ProcessedPostData
{
    public string Description { get; set; } = string.Empty;
    public List<ProcessedPostPartData> Parts { get; set; } = [];
}