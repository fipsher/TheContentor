using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class ProcessedPostPart : BaseEntity
{
    public Guid ProcessedPostId { get; set; }
    public ProcessedPost ProcessedPost { get; set; } = null!;

    public int Part { get; set; }
    public string ProcessedText { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = [];
    public List<SocialPlatform> PublishedTo { get; set; } = [];

    // TTS
    public BlobPath? AudioBlobPath { get; set; }

    // Video
    public BlobPath? VideoBlobPath { get; set; }
    public BlobPath? SubtitleBlobPath { get; set; }
}
