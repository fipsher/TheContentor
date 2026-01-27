namespace TheContentor.Orchestrator.Models.ProcessedPost;

public class ProcessedPostDto
{
    public string Description { get; set; } = string.Empty;
    public List<ProcessedPostPartDto> Parts { get; set; } = [];
}