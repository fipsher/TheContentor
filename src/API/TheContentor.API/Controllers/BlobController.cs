using Microsoft.AspNetCore.Mvc;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlobController(IBlobService blobService) : ControllerBase
{
    [HttpPost("upload")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile file, 
        [FromForm] string containerName)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file uploaded.");
        }

        if (string.IsNullOrEmpty(containerName))
        {
            return BadRequest("Container name is required.");
        }

        await using var stream = file.OpenReadStream();
        var result = await blobService.UploadAsync(
            stream, 
            containerName, 
            file.FileName, 
            file.ContentType);

        return Ok(result);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(
        [FromQuery] string containerName,
        [FromQuery] string blobPath)
    {
        if (string.IsNullOrEmpty(containerName) || string.IsNullOrEmpty(blobPath))
        {
            return BadRequest("Container name and blob path are required.");
        }

        try
        {
            var stream = await blobService.DownloadAsync(containerName, blobPath);

            // Determine content type based on extension
            var extension = Path.GetExtension(blobPath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".mp4" => "video/mp4",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".srt" => "application/x-subrip",
                ".vtt" => "text/vtt",
                _ => "application/octet-stream"
            };

            return File(stream, contentType, Path.GetFileName(blobPath));
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}
