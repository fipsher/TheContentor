using MediatR;
using Microsoft.AspNetCore.Mvc;
using TheContentor.Application.Features.Schedule.Commands;
using TheContentor.Application.Features.Schedule.Models;
using TheContentor.Application.Features.Schedule.Queries;

namespace TheContentor.API.Controllers;

/// <summary>REST API for scheduler operations.</summary>
[ApiController]
[Route("api/[controller]")]
public class ScheduleController(IMediator mediator) : ControllerBase
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
}
