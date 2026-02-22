using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Schedule.Models;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Schedule.Queries;

/// <summary>Returns all scheduled days for the specified month.</summary>
public record GetScheduleQuery(int Year, int Month) : IRequest<List<ScheduledDayDto>>;

/// <summary>Validates <see cref="GetScheduleQuery"/>.</summary>
public class GetScheduleQueryValidator : AbstractValidator<GetScheduleQuery>
{
    /// <summary>Initializes validation rules.</summary>
    public GetScheduleQueryValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2020, 2100);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}

/// <summary>Loads scheduled days for the requested month.</summary>
public class GetScheduleQueryHandler(TheContentorDbContext context)
    : IRequestHandler<GetScheduleQuery, List<ScheduledDayDto>>
{
    /// <summary>Returns a flat list of scheduled calendar days for the month.</summary>
    public async Task<List<ScheduledDayDto>> Handle(GetScheduleQuery request, CancellationToken cancellationToken)
    {
        var startDate = new DateOnly(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return await context.ScheduledPosts
            .AsNoTracking()
            .Where(x => x.ScheduledDate >= startDate && x.ScheduledDate <= endDate)
            .OrderBy(x => x.ScheduledDate)
            .Select(x => new ScheduledDayDto
            {
                ScheduledPostId = x.Id,
                ScheduledDate = x.ScheduledDate,
                SourcePostId = x.SourcePostId,
                Title = x.SourcePost.Title,
                Community = x.SourcePost.Community,
                Score = x.SourcePost.Score,
                IsProcessed = x.SourcePost.ProcessedPost != null,
                IsPosted = x.SourcePost.ProcessedPost != null ? x.SourcePost.ProcessedPost.IsPosted : null,
                CreatedUtc = x.CreatedUtc
            })
            .ToListAsync(cancellationToken);
    }
}
