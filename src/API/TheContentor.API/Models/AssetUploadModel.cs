namespace TheContentor.API.Models;

public class AssetUploadModel
{
    public string? FileName { get; set; }
    public string? Tags { get; set; }
    public bool IsActive { get; set; }
    public IFormFile File { get; set; } = null!;
}