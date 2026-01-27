using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.SourcePosts.Commands;

public record UpdateProcessedPostCommand(
    Guid ProcessedPostId,
    string Title,
    string Description,
    List<string> Hashtags,
    NarratorGender NarratorGender,
    List<UpdateProcessedPostPartDto> Parts) : IRequest;

public record UpdateProcessedPostPartDto(
    Guid? Id,
    string ProcessedText,
    List<string> Hashtags,
    List<SocialPlatform> PublishedTo,
    int Part);

public class UpdateProcessedPostCommandHandler(TheContentorDbContext context)
    : IRequestHandler<UpdateProcessedPostCommand>
{
    public async Task Handle(UpdateProcessedPostCommand request, CancellationToken cancellationToken)
    {
        var processedPost = await context.ProcessedPosts
            .Include(x => x.Parts)
            .FirstOrDefaultAsync(x => x.Id == request.ProcessedPostId, cancellationToken);

        if (processedPost == null)
        {
            return;
        }

        processedPost.Title = request.Title;
        processedPost.Description = request.Description;
        processedPost.Hashtags = request.Hashtags;
        processedPost.NarratorGender = request.NarratorGender;

        // Remove parts that are not in the request
        var partIdsInRequest = request.Parts.Where(p => p.Id.HasValue).Select(p => p.Id!.Value).ToList();
        var partsToRemove = processedPost.Parts.Where(p => !partIdsInRequest.Contains(p.Id)).ToList();
        
        if (partsToRemove.Any())
        {
            context.ProcessedPostParts.RemoveRange(partsToRemove);
        }

        foreach (var partDto in request.Parts)
        {
            if (partDto.Id.HasValue)
            {
                var existingPart = processedPost.Parts.FirstOrDefault(p => p.Id == partDto.Id.Value);
                if (existingPart != null)
                {
                    existingPart.Part = partDto.Part;
                    existingPart.ProcessedText = partDto.ProcessedText;
                    existingPart.Hashtags = partDto.Hashtags;
                    existingPart.PublishedTo = partDto.PublishedTo;
                }
            }
            else
            {
                processedPost.Parts.Add(new ProcessedPostPart
                {
                    ProcessedPostId = processedPost.Id,
                    Part = partDto.Part,
                    ProcessedText = partDto.ProcessedText,
                    Hashtags = partDto.Hashtags,
                    PublishedTo = partDto.PublishedTo
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
