using TheContentor.Orchestrator.Models.TTS;

namespace TheContentor.Orchestrator.Models.Video;

public class VideoGenerationData
{
    public List<VideoPartData> Parts { get; set; } = [];
    public List<AssetData> Assets { get; set; } = [];
}

public class VideoPartData
{
    public Guid Id { get; set; }
    public int Part { get; set; }
    public BlobPathInfo AudioBlobPath { get; set; } = null!;
    public TimeSpan AudioDuration { get; set; }
}

public class AssetData
{
    public Guid Id { get; set; }
    public BlobPathInfo BlobPath { get; set; } = null!;
    public TimeSpan? Duration { get; set; }
}
