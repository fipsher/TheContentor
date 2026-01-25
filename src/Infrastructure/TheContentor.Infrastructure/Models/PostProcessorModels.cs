namespace TheContentor.Infrastructure.Models;

public class ProcessedPostResponse
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = [];
    public List<ProcessedPostPartResponse> Parts { get; set; } = [];
}

public class ProcessedPostPartResponse
{
    public int Part { get; set; }
    public string ProcessedText { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = [];
}
