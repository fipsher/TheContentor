using MediatR;
using Microsoft.AspNetCore.Mvc;
using TheContentor.API.Models;
using TheContentor.Application.Features.Assets.Commands;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Application.Features.Assets.Queries;
using TheContentor.Application.Features.Assets.Queries.GetYouTubeVideoMetadata;

namespace TheContentor.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssetController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AssetDto>>> GetList()
    {
        return await mediator.Send(new GetAssetListQuery());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AssetDto>> GetById(Guid id)
    {
        var result = await mediator.Send(new GetAssetByIdQuery(id));
        if (result == null)
            return NotFound();

        return result;
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<Guid>> Create([FromForm] AssetUploadModel model)
    {
        await using var stream = model.File.OpenReadStream();
        var command = new CreateAssetCommand
        {
            Name = model.FileName ?? model.File.FileName,
            Tags = model.Tags ?? string.Empty,
            FileStream = stream,
            ContentType = model.File.ContentType
        };

        var id = await mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Returns the title and available quality tiers for a YouTube video before upload.</summary>
    [HttpGet("youtube-metadata")]
    public async Task<ActionResult<YouTubeVideoMetadataDto>> GetYouTubeMetadata([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("url is required.");

        var result = await mediator.Send(new GetYouTubeVideoMetadataQuery(url));
        return Ok(result);
    }

    /// <summary>Downloads a YouTube video and stores it as an asset.</summary>
    [HttpPost("youtube")]
    public async Task<ActionResult<Guid>> UploadYouTube([FromBody] YouTubeAssetUploadModel model)
    {
        var id = await mediator.Send(new UploadYouTubeAssetCommand(model.YouTubeUrl, model.Name, model.Quality));
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult> ToggleStatus(Guid id)
    {
        var result = await mediator.Send(new ToggleAssetStatusCommand(id));
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>Renames an asset and updates its tags.</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Rename(Guid id, [FromBody] RenameAssetRequest request)
    {
        var ok = await mediator.Send(new RenameAssetCommand(id, request.Name, request.Tags));
        if (!ok) return NotFound();
        return NoContent();
    }
}

/// <summary>Request model for renaming an asset.</summary>
public class RenameAssetRequest
{
    /// <summary>New asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Updated tag string.</summary>
    public string Tags { get; set; } = string.Empty;
}
