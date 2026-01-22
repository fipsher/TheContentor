using TheContentor.Domain.Common;
using TheContentor.Domain.Enums;

namespace TheContentor.Domain.Entities;

public class VideoProject : BaseEntity
{
    public SourcePost SourcePost { get; set; } = null!;
    public VideoProjectStatus Status { get; set; }
    
    // Configuration Snapshot
    public Asset Asset { get; set; } = null!;
    public TtsEngine TtsEngine { get; set; }
    public string VoiceProfileJson { get; set; } = string.Empty;
    public SubtitleEngine SubtitleEngine { get; set; }
    public SubtitleStyle SubStyle { get; set; }

    // Artifacts
    public string GeneratedAudioPath { get; set; } = string.Empty;
    public string SubtitleFilePath { get; set; } = string.Empty;
    public string FinalVideoPath { get; set; } = string.Empty;

    // Orchestration Metadata
    public DateTime CreatedAt { get; set; }
    public string ErrorLog { get; set; } = string.Empty;
}
