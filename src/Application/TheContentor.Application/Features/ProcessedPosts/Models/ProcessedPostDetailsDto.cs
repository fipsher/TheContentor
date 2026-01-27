using TheContentor.Application.Features.SourcePosts.Models;
using TheContentor.Domain.Enums;

namespace TheContentor.Application.Features.ProcessedPosts.Models;

public class ProcessedPostDetailsDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = [];
    public List<ProcessedPostPartDto> Parts { get; set; } = [];
    public NarratorGender NarratorGender { get; set; }
    public TtsStatus TtsStatus { get; set; }
    public string? TtsSettings { get; set; }
    public BlobPathDto? DescriptionAudioBlobPath { get; set; }
}
