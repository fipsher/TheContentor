using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Schedule.Commands;

/// <summary>Removes a scheduled post entry by its identifier.</summary>
public record RemoveScheduledPostCommand(Guid ScheduledPostId) : IRequest<bool>;

/// <summary>Validates <see cref="RemoveScheduledPostCommand"/>.</summary>
public class RemoveScheduledPostCommandValidator : AbstractValidator<RemoveScheduledPostCommand>
{
    /// <summary>Initializes validation rules.</summary>
    public RemoveScheduledPostCommandValidator()
    {
        RuleFor(x => x.ScheduledPostId).NotEmpty();
    }
}

/// <summary>Handles removal of a scheduled post entry.</summary>
public class RemoveScheduledPostCommandHandler(TheContentorDbContext context)
    : IRequestHandler<RemoveScheduledPostCommand, bool>
{
    /// <summary>Deletes the entry and returns whether it existed.</summary>
    public async Task<bool> Handle(RemoveScheduledPostCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.ScheduledPosts
            .FirstOrDefaultAsync(x => x.Id == request.ScheduledPostId, cancellationToken);

        if (entity == null) return false;

        context.ScheduledPosts.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
