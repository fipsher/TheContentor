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
}
