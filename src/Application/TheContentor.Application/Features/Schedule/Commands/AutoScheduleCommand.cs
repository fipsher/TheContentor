using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Schedule.Commands;

/// <summary>Automatically fills empty calendar days in a date range with eligible approved posts.</summary>
public record AutoScheduleCommand(DateOnly StartDate, DateOnly EndDate, string? CommunityFilter) : IRequest<int>;

/// <summary>Validates <see cref="AutoScheduleCommand"/> inputs.</summary>
public class AutoScheduleCommandValidator : AbstractValidator<AutoScheduleCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public AutoScheduleCommandValidator()
    {
        RuleFor(x => x.StartDate)
            .NotEqual(default(DateOnly))
            .WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .NotEqual(default(DateOnly))
            .WithMessage("EndDate is required.")
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be on or after StartDate.");

        RuleFor(x => x)
            .Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 366)
            .WithMessage("Date range must not exceed 366 days.")
            .When(x => x.StartDate != default && x.EndDate != default);

        RuleFor(x => x.CommunityFilter)
            .MaximumLength(128)
            .When(x => x.CommunityFilter != null);
    }
}

/// <summary>Handles automatic scheduling of unposted posts into empty calendar days.</summary>
public class AutoScheduleCommandHandler(TheContentorDbContext context)
    : IRequestHandler<AutoScheduleCommand, int>
{
    /// <summary>Fills empty days in the range and returns the count of newly scheduled posts.</summary>
    public async Task<int> Handle(AutoScheduleCommand request, CancellationToken cancellationToken)
    {
        var occupiedDates = await context.ScheduledPosts
            .Where(x => x.ScheduledDate >= request.StartDate && x.ScheduledDate <= request.EndDate)
            .Select(x => x.ScheduledDate)
            .ToHashSetAsync(cancellationToken);

        var emptyDays = Enumerable
            .Range(0, request.EndDate.DayNumber - request.StartDate.DayNumber + 1)
            .Select(offset => request.StartDate.AddDays(offset))
            .Where(day => !occupiedDates.Contains(day))
            .ToList();

        if (emptyDays.Count == 0) return 0;

        var alreadyScheduledPostIds = await context.ScheduledPosts
            .Select(x => x.SourcePostId)
            .ToHashSetAsync(cancellationToken);

        var communityFilter = string.IsNullOrWhiteSpace(request.CommunityFilter)
            ? null
            : request.CommunityFilter.ToLower();

        var eligiblePostIds = await context.SourcePosts
            .AsNoTracking()
            .Where(x =>
                x.Status != SourcePostStatus.Skipped &&
                (x.ProcessedPost == null || !x.ProcessedPost.IsPosted) &&
                !alreadyScheduledPostIds.Contains(x.Id) &&
                (communityFilter == null || x.Community.ToLower() == communityFilter))
            .OrderByDescending(x => x.Score)
            .Take(emptyDays.Count)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var newEntries = eligiblePostIds
            .Select((postId, i) => new ScheduledPost
            {
                Id = Guid.NewGuid(),
                ScheduledDate = emptyDays[i],
                SourcePostId = postId,
                CreatedUtc = DateTimeOffset.UtcNow
            })
            .ToList();

        context.ScheduledPosts.AddRange(newEntries);
        await context.SaveChangesAsync(cancellationToken);

        return newEntries.Count;
    }
}
