using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TheContentor.API.Hubs;
using TheContentor.Application.Features.ProcessedPosts.Commands;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Application.Features.ProcessedPosts.Queries;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;

namespace TheContentor.API.Controllers;

/// <summary>Controller for ProcessedPost operations.</summary>
[ApiController]
[Route("api/[controller]")]
public class ProcessedPostController(IMediator mediator, IHubContext<VideoGenerationHub> hubContext) : ControllerBase
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

    /// <summary>Triggers the full TTS + Video generation pipeline.</summary>
    [HttpPost("{id:guid}/generate-all")]
    public async Task<IActionResult> GenerateAll(Guid id, [FromBody] GenerateAllSettingsModel settings)
    {
        await mediator.Send(new GenerateAllCommand(id, settings.TtsSettings, settings.AssetIds));
        return Accepted();
    }

    /// <summary>Cancel video generation for a ProcessedPost.</summary>
    [HttpPost("{id:guid}/cancel-video")]
    public async Task<IActionResult> CancelVideo(Guid id)
    {
        await mediator.Send(new CancelVideoCommand(id));
        return Accepted();
    }

    /// <summary>Cancels an in-progress generate-all pipeline.</summary>
    [HttpPost("{id:guid}/cancel-generation")]
    public async Task<IActionResult> CancelGeneration(Guid id)
    {
        await mediator.Send(new CancelGenerationCommand(id));
        return Accepted();
    }

    /// <summary>Receives progress updates from the orchestrator and broadcasts via SignalR.</summary>
    [HttpPost("progress")]
    public async Task<IActionResult> ReportProgress([FromBody] GenerationProgressModel progress)
    {
        await hubContext.Clients.Group(progress.ProcessedPostId.ToString())
            .SendAsync("ProgressUpdate", progress);
        return Ok();
    }

    /// <summary>Cleans up intermediate assets for a processed post.</summary>
    [HttpPost("{id:guid}/cleanup-intermediate")]
    public async Task<IActionResult> CleanupIntermediateAssets(
        Guid id,
        [FromBody] CleanupIntermediateRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        await mediator.Send(new CleanupIntermediateAssetsCommand(id, request?.AdditionalBlobPaths), cancellationToken);
        return Ok();
    }

    /// <summary>Cleans up all generated assets (TTS audio, subtitles, video) for a processed post.</summary>
    [HttpPost("{id:guid}/cleanup")]
    public async Task<IActionResult> CleanupAssets(Guid id)
    {
        try
        {
            await mediator.Send(new CleanupPreviousAssetsCommand(id));
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Sets the posted status for a processed post.</summary>
    [HttpPatch("{id:guid}/mark-posted")]
    public async Task<IActionResult> MarkPosted(Guid id, [FromBody] MarkPostedRequest request)
    {
        var ok = await mediator.Send(new MarkProcessedPostAsPostedCommand(id, request.IsPosted));
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>Toggles a social platform published status for a specific part.</summary>
    [HttpPatch("parts/{partId:guid}/toggle-platform")]
    public async Task<IActionResult> TogglePartPlatform(
        Guid partId, [FromBody] TogglePartPlatformRequest request)
    {
        var ok = await mediator.Send(
            new TogglePartPlatformCommand(partId, request.Platform, request.IsPublished));
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>Update TTS status (called by orchestrator).</summary>
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

        var partAudioDurations = request.PartAudioBlobPaths?
            .ToDictionary(
                x => x.Key,
                x => x.Value.AudioDurationSeconds
            ) ?? new Dictionary<Guid, double?>();

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
            partBlobPaths,
            partAudioDurations
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

/// <summary>Blob path with optional audio duration.</summary>
public class BlobPathRequest
{
    /// <summary>Blob container name.</summary>
    public string ContainerName { get; set; } = string.Empty;
    /// <summary>Asset path within the container.</summary>
    public string AssetPath { get; set; } = string.Empty;
    /// <summary>Audio duration in seconds (from TTS worker).</summary>
    public double? AudioDurationSeconds { get; set; }
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

/// <summary>Request model for marking a post as posted.</summary>
public class MarkPostedRequest
{
    /// <summary>Desired posted state.</summary>
    public bool IsPosted { get; set; }
}

/// <summary>Request model for toggling a platform on a part.</summary>
public class TogglePartPlatformRequest
{
    /// <summary>The social platform to toggle.</summary>
    public SocialPlatform Platform { get; set; }
    /// <summary>Whether the part is published to the platform.</summary>
    public bool IsPublished { get; set; }
}

/// <summary>Optional body for the cleanup-intermediate endpoint carrying extra blobs to delete.</summary>
public record CleanupIntermediateRequest(List<BlobPath>? AdditionalBlobPaths);
