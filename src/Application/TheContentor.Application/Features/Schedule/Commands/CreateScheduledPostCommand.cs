using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Schedule.Commands;

/// <summary>Manually assigns a source post to a specific calendar day.</summary>
public record CreateScheduledPostCommand(DateOnly ScheduledDate, Guid SourcePostId) : IRequest<Guid>;

/// <summary>Validates <see cref="CreateScheduledPostCommand"/> surface-level inputs.</summary>
public class CreateScheduledPostCommandValidator : AbstractValidator<CreateScheduledPostCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public CreateScheduledPostCommandValidator()
    {
        RuleFor(x => x.ScheduledDate)
            .NotEqual(default(DateOnly))
            .WithMessage("ScheduledDate is required.");
        RuleFor(x => x.SourcePostId)
            .NotEmpty();
    }
}

/// <summary>Handles manual scheduling of a source post to a calendar day.</summary>
public class CreateScheduledPostCommandHandler(TheContentorDbContext context)
    : IRequestHandler<CreateScheduledPostCommand, Guid>
{
    /// <summary>Validates business rules, creates the schedule entry, and returns its identifier.</summary>
    public async Task<Guid> Handle(CreateScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var sourcePost = await context.SourcePosts
            .Include(x => x.ProcessedPost)
            .FirstOrDefaultAsync(x => x.Id == request.SourcePostId, cancellationToken);

        if (sourcePost == null)
            throw new InvalidOperationException("SourcePost not found.");

        if (sourcePost.ProcessedPost?.IsPosted == true)
            throw new InvalidOperationException("Cannot schedule a post that is already marked as posted.");

        var alreadyScheduled = await context.ScheduledPosts
            .AnyAsync(x => x.SourcePostId == request.SourcePostId, cancellationToken);

        if (alreadyScheduled)
            throw new InvalidOperationException("This post is already scheduled on another day.");

        var entity = new ScheduledPost
        {
            Id = Guid.NewGuid(),
            ScheduledDate = request.ScheduledDate,
            SourcePostId = request.SourcePostId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        context.ScheduledPosts.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
