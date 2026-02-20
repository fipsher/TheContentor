using TheContentor.Domain.Enums;

namespace TheContentor.API.Models;

public class YouTubeAssetUploadModel
{
    public string YouTubeUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public YouTubeVideoQuality Quality { get; set; } = YouTubeVideoQuality.P1080;
}
