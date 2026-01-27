using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class ProcessedPost : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public List<string> Hashtags { get; set; } = [];
    public List<ProcessedPostPart> Parts { get; set; } = [];
    public NarratorGender NarratorGender { get; set; }

    // TTS
    public TtsStatus TtsStatus { get; set; } = TtsStatus.NotGenerated;
    public string? TtsSettings { get; set; }
    public BlobPath? DescriptionAudioBlobPath { get; set; }
}
