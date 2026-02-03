using MediatR;
using Microsoft.AspNetCore.Mvc;
using TheContentor.Application.Features.ProcessedPosts.Commands;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Application.Features.ProcessedPosts.Queries;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;

namespace TheContentor.API.Controllers;

/// <summary>
/// Controller for ProcessedPost operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProcessedPostController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Get a ProcessedPost by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcessedPostDetailsDto>> GetById(Guid id)
    {
        var result = await mediator.Send(new GetProcessedPostByIdQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate TTS for a ProcessedPost
    /// </summary>
    [HttpPost("{id:guid}/generate-tts")]
    public async Task<IActionResult> GenerateTts(Guid id, [FromBody] TtsSettingsModel settings)
    {
        await mediator.Send(new GenerateTtsCommand(id, settings));
        return Accepted();
    }

    /// <summary>
    /// Update TTS status (called by orchestrator)
    /// </summary>
    [HttpPut("tts-status")]
    public async Task<IActionResult> UpdateTtsStatus([FromBody] UpdateTtsStatusRequest request)
    {
        var partBlobPaths = request.PartAudioBlobPaths?
            .ToDictionary(
                x => x.Key,
                x => new BlobPath
                {
                    ContainerName = x.Value.ContainerName,
                    AssetPath = x.Value.AssetPath
                }
            ) ?? new Dictionary<Guid, BlobPath>();

        var descriptionBlobPath = request.DescriptionAudioBlobPath == null
            ? null
            : new BlobPath
            {
                ContainerName = request.DescriptionAudioBlobPath.ContainerName,
                AssetPath = request.DescriptionAudioBlobPath.AssetPath
            };

        await mediator.Send(new UpdateTtsStatusCommand(
            request.ProcessedPostId,
            (TtsStatus)request.Status,
            descriptionBlobPath,
            partBlobPaths
        ));

        return Ok();
    }

    /// <summary>
    /// Update video status (called by orchestrator)
    /// </summary>
    [HttpPut("video-status")]
    public async Task<IActionResult> UpdateVideoStatus([FromBody] UpdateVideoStatusRequest request)
    {
        var partBlobPaths = request.PartVideoBlobPaths?
            .ToDictionary(
                x => x.Key,
                x => new BlobPath
                {
                    ContainerName = x.Value.ContainerName,
                    AssetPath = x.Value.AssetPath
                }
            ) ?? new Dictionary<Guid, BlobPath>();

        await mediator.Send(new UpdateVideoStatusCommand(
            request.ProcessedPostId,
            (VideoStatus)request.Status,
            partBlobPaths
        ));

        return Ok();
    }
}

/// <summary>
/// Request model for updating TTS status
/// </summary>
public class UpdateTtsStatusRequest
{
    public Guid ProcessedPostId { get; set; }
    public int Status { get; set; }
    public BlobPathRequest? DescriptionAudioBlobPath { get; set; }
    public Dictionary<Guid, BlobPathRequest>? PartAudioBlobPaths { get; set; }
}

public class BlobPathRequest
{
    public string ContainerName { get; set; } = string.Empty;
    public string AssetPath { get; set; } = string.Empty;
}

/// <summary>
/// Request model for updating video status
/// </summary>
public class UpdateVideoStatusRequest
{
    public Guid ProcessedPostId { get; set; }
    public int Status { get; set; }
    public Dictionary<Guid, BlobPathRequest>? PartVideoBlobPaths { get; set; }
}
