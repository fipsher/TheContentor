using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TheContentor.API.Hubs;
using TheContentor.Application.Features.Schedule.Commands;
using TheContentor.Application.Features.Schedule.Models;
using TheContentor.Application.Features.Schedule.Queries;

namespace TheContentor.API.Controllers;

/// <summary>REST API for scheduler operations.</summary>
[ApiController]
[Route("api/[controller]")]
public class ScheduleController(IMediator mediator, IHubContext<VideoGenerationHub> hubContext) : ControllerBase
{
    /// <summary>Gets all scheduled days for the specified year and month.</summary>
    [HttpGet]
    public async Task<ActionResult<List<ScheduledDayDto>>> GetSchedule(
        [FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetScheduleQuery(year, month), cancellationToken);
        return Ok(result);
    }

    /// <summary>Manually assigns a processed post to a specific calendar day.</summary>
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateScheduledPostCommand command, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(command, cancellationToken);
        return Ok(id);
    }

    /// <summary>Removes a scheduled post entry.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, CancellationToken cancellationToken)
    {
        var ok = await mediator.Send(new RemoveScheduledPostCommand(id), cancellationToken);
        if (!ok) return NotFound();
        return NoContent();
    }

    /// <summary>Automatically fills empty days in the given date range with top-scored approved posts.</summary>
    [HttpPost("auto")]
    public async Task<ActionResult<int>> AutoSchedule(
        [FromBody] AutoScheduleCommand command, CancellationToken cancellationToken)
    {
        var count = await mediator.Send(command, cancellationToken);
        return Ok(count);
    }

    /// <summary>Triggers bulk AI processing + video generation for all eligible posts in a week.</summary>
    [HttpPost("generate-week")]
    public async Task<ActionResult<int>> GenerateWeek(
        [FromBody] GenerateWeekCommand command, CancellationToken cancellationToken)
    {
        var count = await mediator.Send(command, cancellationToken);
        return Ok(count);
    }

    /// <summary>Receives generate-week progress updates from the orchestrator and broadcasts via SignalR.</summary>
    [HttpPost("generate-week-progress")]
    public async Task<IActionResult> ReportGenerateWeekProgress(
        [FromBody] GenerateWeekProgressPayload payload, CancellationToken cancellationToken)
    {
        await hubContext.Clients.Group($"generate-week-{payload.WeekStart}")
            .SendAsync("GenerateWeekUpdate", payload, cancellationToken);
        return Ok();
    }
}

/// <summary>Payload for generate-week progress updates from the orchestrator.</summary>
public class GenerateWeekProgressPayload
{
    /// <summary>Week start date (ISO format).</summary>
    public string WeekStart { get; set; } = string.Empty;
    /// <summary>Total number of posts in the batch.</summary>
    public int TotalPosts { get; set; }
    /// <summary>Number of posts completed so far.</summary>
    public int CompletedPosts { get; set; }
    /// <summary>Source post ID currently being processed.</summary>
    public Guid? CurrentSourcePostId { get; set; }
    /// <summary>Current stage description.</summary>
    public string Stage { get; set; } = string.Empty;
    /// <summary>True when all posts are done.</summary>
    public bool IsComplete { get; set; }
    /// <summary>True if any post failed.</summary>
    public bool HasError { get; set; }
    /// <summary>Error message if HasError.</summary>
    public string? ErrorMessage { get; set; }
}
