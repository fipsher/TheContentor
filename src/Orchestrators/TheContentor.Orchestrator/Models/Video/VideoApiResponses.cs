namespace TheContentor.Orchestrator.Models.Video;

public class ProcessedPostDetailsResponse
{
    public List<ProcessedPostPartResponse> Parts { get; set; } = [];
}

public class ProcessedPostPartResponse
{
    public Guid? Id { get; set; }
    public int Part { get; set; }
    public BlobPathResponse? AudioBlobPath { get; set; }
}

public class AssetDetailsResponse
{
    public BlobPathResponse? BlobPath { get; set; }
    public string? Duration { get; set; }
}

public class BlobPathResponse
{
    public string ContainerName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
}
