namespace TheContentor.Orchestrator.Models.TTS;

public class BlobPathInfo
{
    public string ContainerName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
    public Guid? PartId { get; set; }
    public string TextType { get; set; } = string.Empty;
    public double? AudioDurationSeconds { get; set; }
}