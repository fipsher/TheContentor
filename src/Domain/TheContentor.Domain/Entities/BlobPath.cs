namespace TheContentor.Domain.Entities;

public record BlobPath
{
    public string ContainerName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
}