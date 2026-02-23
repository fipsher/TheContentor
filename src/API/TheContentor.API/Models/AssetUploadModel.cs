using TheContentor.Domain.Enums;

namespace TheContentor.API.Models;

public class AssetUploadModel
{
    public string? FileName { get; set; }
    public AssetContentTag? ContentTag { get; set; }
    public IFormFile File { get; set; } = null!;
}