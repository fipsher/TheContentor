using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.ProcessedPosts.Models;

/// <summary>Detailed view of a processed post with audio metadata.</summary>
public class ProcessedPostDetailsDto
{
    /// <summary>Processed post identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Post title used for output.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Full description text.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Hashtags derived for the post.</summary>
    public List<string> Hashtags { get; set; } = [];
    /// <summary>Per-part processed content details.</summary>
    public List<ProcessedPostPartDto> Parts { get; set; } = [];
    /// <summary>Narrator voice gender selection.</summary>
    public NarratorGender NarratorGender { get; set; }
    /// <summary>Current text-to-speech generation status.</summary>
    public TtsStatus TtsStatus { get; set; }
    /// <summary>Serialized TTS settings payload.</summary>
    public string? TtsSettings { get; set; }
    /// <summary>Audio blob path for the description narration.</summary>
    public BlobPathDto? DescriptionAudioBlobPath { get; set; }
}
