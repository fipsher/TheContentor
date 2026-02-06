using MediatR;
using Microsoft.AspNetCore.Mvc;
using TheContentor.API.Models;
using TheContentor.Application.Features.Assets.Commands;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Application.Features.Assets.Queries;

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
        {
            return NotFound();
        }

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

    [HttpPost("youtube-upload")]
    public async Task<ActionResult<Guid>> UploadYouTubeAsset([FromBody] YouTubeAssetUploadModel model)
    {
        try
        {
            var command = new UploadYouTubeAssetCommand(model.YouTubeUrl);
            var id = await mediator.Send(command);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            // This would catch failures from metadata retrieval, stream download, or blob storage upload
            return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        }
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult> ToggleStatus(Guid id)
    {
        var result = await mediator.Send(new ToggleAssetStatusCommand(id));
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
