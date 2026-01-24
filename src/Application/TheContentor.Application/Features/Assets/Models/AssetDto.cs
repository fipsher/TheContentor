using TheContentor.Domain.Entities;

namespace TheContentor.Application.Features.Assets.Models;

public record AssetDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public bool IsActive { get; set; }
    public BlobPath BlobPath { get; set; } = null!;
    public Uri SasUri { get; set; } = null!;
}
