using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.SourcePosts.Commands;

/// <summary>Processes a source post into a processed post.</summary>
public record ProcessSourcePostCommand(Guid SourcePostId) : IRequest;

/// <summary>Runs processing for a source post and stores results.</summary>
public class ProcessSourcePostCommandHandler(TheContentorDbContext context, IPostProcessor postProcessor)
    : IRequestHandler<ProcessSourcePostCommand>
{
    /// <summary>Invokes the processor and persists the processed post.</summary>
    public async Task Handle(ProcessSourcePostCommand request, CancellationToken cancellationToken)
    {
        var sourcePost = await context.SourcePosts
            .Include(x => x.ProcessedPost)
            .ThenInclude(x => x!.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.SourcePostId, cancellationToken);

        if (sourcePost == null)
        {
            return;
        }

        if (sourcePost.ProcessedPost != null)
        {
            context.ProcessedPosts.Remove(sourcePost.ProcessedPost);
        }

        var processedData = await postProcessor.ProcessAsync(sourcePost.Title, sourcePost.RawText, cancellationToken);

        var processedPost = new ProcessedPost
        {
            Id = sourcePost.Id,
            Title = processedData.Title,
            NarratorGender = Enum.TryParse<NarratorGender>(processedData.NarratorGender, out var gender) ? gender : NarratorGender.Male,
            Description = processedData.Description,
            Hashtags = processedData.Hashtags,
            Parts = processedData.Parts.Select(p => new ProcessedPostPart
            {
                Part = p.Part,
                ProcessedText = p.ProcessedText,
                Hashtags = p.Hashtags
            }).ToList()
        };

        sourcePost.Status = SourcePostStatus.Processed;
        context.ProcessedPosts.Add(processedPost);

        await context.SaveChangesAsync(cancellationToken);
    }
}
