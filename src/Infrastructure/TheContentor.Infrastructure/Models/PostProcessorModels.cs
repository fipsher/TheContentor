namespace TheContentor.Infrastructure.Models;

public class ProcessedPostResponse
{
    public List<string> Hashtags { get; set; } = new();
    public List<ProcessedPostPartResponse> Parts { get; set; } = new();
}

public class ProcessedPostPartResponse
{
    public string ProcessedText { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
}
