using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.ProcessedPosts.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Schedule.Commands;

/// <summary>Triggers bulk AI processing + video generation for all eligible posts in the given week.</summary>
public record GenerateWeekCommand(DateOnly WeekStart) : IRequest<int>;

/// <summary>Finds eligible posts, assigns unique assets, and enqueues the generate-week orchestration.</summary>
public class GenerateWeekCommandHandler(
    TheContentorDbContext context,
    ServiceBusClient serviceBusClient) : IRequestHandler<GenerateWeekCommand, int>
{
    /// <summary>Returns the number of posts queued for processing.</summary>
    public async Task<int> Handle(GenerateWeekCommand request, CancellationToken cancellationToken)
    {
        var weekEnd = request.WeekStart.AddDays(6);

        // Get all scheduled posts for the week that are not posted and don't have video generated
        var scheduledPosts = await context.ScheduledPosts
            .Include(sp => sp.SourcePost)
                .ThenInclude(s => s.ProcessedPost)
            .Where(sp => sp.ScheduledDate >= request.WeekStart && sp.ScheduledDate <= weekEnd)
            .Where(sp => sp.SourcePost.ProcessedPost == null
                || sp.SourcePost.ProcessedPost.IsPosted != true)
            .Where(sp => sp.SourcePost.ProcessedPost == null
                || sp.SourcePost.ProcessedPost.VideoStatus != VideoStatus.Generated)
            // Exclude posts already in progress
            .Where(sp => sp.SourcePost.Status != SourcePostStatus.Processing)
            .Where(sp => sp.SourcePost.ProcessedPost == null
                || (sp.SourcePost.ProcessedPost.TtsStatus != TtsStatus.InProgress
                    && sp.SourcePost.ProcessedPost.VideoStatus != VideoStatus.InProgress))
            .OrderBy(sp => sp.ScheduledDate)
            .ThenBy(sp => sp.CreatedUtc)
            .ToListAsync(cancellationToken);

        if (scheduledPosts.Count == 0)
            return 0;

        // Get available assets sorted by last used (null first = never used, then oldest used)
        var assets = await context.Assets
            .Where(a => a.IsActive)
            .OrderBy(a => a.LastUsedAt.HasValue ? 1 : 0)
            .ThenBy(a => a.LastUsedAt)
            .ToListAsync(cancellationToken);

        if (assets.Count == 0)
            throw new InvalidOperationException("No active video assets found. Please upload video assets first.");

        // Build the list of items for the orchestrator
        var items = new List<GenerateWeekItem>();
        var now = DateTime.UtcNow;

        for (var i = 0; i < scheduledPosts.Count; i++)
        {
            var sp = scheduledPosts[i];
            var processedPost = sp.SourcePost.ProcessedPost;
            var needsAiProcessing = processedPost == null;

            // Assign unique asset (round-robin if more posts than assets)
            var asset = assets[i % assets.Count];

            // Determine voice based on narrator gender
            var gender = processedPost?.NarratorGender ?? NarratorGender.Male;
            var voice = gender == NarratorGender.Male ? "am_adam" : "af_heart";

            items.Add(new GenerateWeekItem
            {
                SourcePostId = sp.SourcePostId,
                ProcessedPostId = processedPost?.Id,
                NeedsAiProcessing = needsAiProcessing,
                AssetId = asset.Id,
                Voice = voice,
                NarratorGender = gender
            });

            // Mark asset as used
            asset.LastUsedAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);

        // Send to orchestration trigger queue
        var sender = serviceBusClient.CreateSender("trigger-orchestration-queue");
        try
        {
            var payload = new
            {
                WeekStart = request.WeekStart.ToString("yyyy-MM-dd"),
                Items = items.Select(item => new
                {
                    item.SourcePostId,
                    item.ProcessedPostId,
                    item.NeedsAiProcessing,
                    item.AssetId,
                    item.Voice,
                    NarratorGender = item.NarratorGender.ToString()
                })
            };

            var message = new ServiceBusMessage(JsonSerializer.Serialize(payload))
            {
                ContentType = "application/json",
                ApplicationProperties =
                {
                    ["Type"] = "generate-week",
                },
            };

            await sender.SendMessageAsync(message, cancellationToken);
        }
        finally
        {
            await sender.DisposeAsync();
        }

        return items.Count;
    }
}

/// <summary>A single post item within the generate-week batch.</summary>
public class GenerateWeekItem
{
    /// <summary>Source post identifier.</summary>
    public Guid SourcePostId { get; set; }
    /// <summary>Processed post identifier. Null if AI processing is needed first.</summary>
    public Guid? ProcessedPostId { get; set; }
    /// <summary>Whether this post needs AI processing before video generation.</summary>
    public bool NeedsAiProcessing { get; set; }
    /// <summary>Background video asset to use for this post.</summary>
    public Guid AssetId { get; set; }
    /// <summary>Kokoro voice identifier.</summary>
    public string Voice { get; set; } = string.Empty;
    /// <summary>Narrator gender for this post.</summary>
    public NarratorGender NarratorGender { get; set; }
}
