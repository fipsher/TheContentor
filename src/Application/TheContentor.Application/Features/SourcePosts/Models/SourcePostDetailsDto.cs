using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.SourcePosts.Models;

/// <summary>Full details of a source post.</summary>
public class SourcePostDetailsDto
{
    /// <summary>Source post identifier.</summary>
    public Guid Id { get; set; }
    /// <summary>Origin platform for the post.</summary>
    public SourcePlatform Platform { get; set; }
    /// <summary>Community or subreddit name.</summary>
    public string Community { get; set; } = string.Empty;
    /// <summary>Post title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Author or poster name.</summary>
    public string AuthorName { get; set; } = string.Empty;
    /// <summary>Raw text content.</summary>
    public string RawText { get; set; } = string.Empty;
    /// <summary>Word count of the raw text.</summary>
    public int WordCount { get; set; }
    /// <summary>Score reported by the platform.</summary>
    public int Score { get; set; }
    /// <summary>Upvote ratio when available.</summary>
    public double? UpvoteRatio { get; set; }
    /// <summary>Original creation time (UTC).</summary>
    public DateTimeOffset CreatedUtc { get; set; }
    /// <summary>External URL to the original post.</summary>
    public string ExternalUrl { get; set; } = string.Empty;
    /// <summary>Processing status.</summary>
    public SourcePostStatus Status { get; set; }
    /// <summary>Linked processed post details when available.</summary>
    public ProcessedPostDto? ProcessedPost { get; set; }
}

/// <summary>Processed post metadata for a source post.</summary>
public class ProcessedPostDto
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

/// <summary>Processed post segment details.</summary>
public class ProcessedPostPartDto
{
    /// <summary>Processed part identifier when persisted.</summary>
    public Guid? Id { get; set; }
    /// <summary>Processed text for the part.</summary>
    public string ProcessedText { get; set; } = string.Empty;
    /// <summary>Hashtags for the part.</summary>
    public List<string> Hashtags { get; set; } = [];
    /// <summary>Platforms where the part is published.</summary>
    public List<SocialPlatform> PublishedTo { get; set; } = [];
    /// <summary>Sequence index for the part.</summary>
    public int Part { get; set; }
    /// <summary>Audio blob path for the part narration.</summary>
    public BlobPathDto? AudioBlobPath { get; set; }
}

/// <summary>Blob location and access info for media.</summary>
public class BlobPathDto
{
    /// <summary>Blob container name.</summary>
    public string ContainerName { get; set; } = string.Empty;
    /// <summary>Path to the asset within the container.</summary>
    public string AssetPath { get; set; } = string.Empty;
    /// <summary>SAS URL when access is requested.</summary>
    public string? SasUrl { get; set; }
}
