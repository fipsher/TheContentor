namespace TheContentor.Application.Features.Assets.Models;

public record AssetDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsActive { get; set; }
}
