namespace TheContentor.Orchestrator.Models.ProcessedPost;

public class ProcessedPostDto
{
    /// <summary>Processed post subject/title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Full post description text.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Processed parts for narration.</summary>
    public List<ProcessedPostPartDto> Parts { get; set; } = [];
}
