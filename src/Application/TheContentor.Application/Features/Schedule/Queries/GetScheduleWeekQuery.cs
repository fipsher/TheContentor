using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Application.Features.Schedule.Models;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Schedule.Queries;

/// <summary>Returns all scheduled days for the specified week (7 days starting from <see cref="WeekStart"/>).</summary>
public record GetScheduleWeekQuery(DateOnly WeekStart) : IRequest<List<ScheduledDayDto>>;

/// <summary>Loads scheduled days for the requested week and resolves video SAS URLs for generated videos.</summary>
public class GetScheduleWeekQueryHandler(TheContentorDbContext context, IBlobService blobService)
    : IRequestHandler<GetScheduleWeekQuery, List<ScheduledDayDto>>
{
    /// <summary>Returns a flat list of scheduled calendar days for the week, with video URLs resolved where available.</summary>
    public async Task<List<ScheduledDayDto>> Handle(GetScheduleWeekQuery request, CancellationToken cancellationToken)
    {
        var weekEnd = request.WeekStart.AddDays(6);

        var rows = await context.ScheduledPosts
            .AsNoTracking()
            .Where(x => x.ScheduledDate >= request.WeekStart && x.ScheduledDate <= weekEnd)
            .OrderBy(x => x.ScheduledDate)
            .Select(x => new
            {
                ScheduledPostId = x.Id,
                x.ScheduledDate,
                x.SourcePostId,
                x.SourcePost.Title,
                x.SourcePost.Community,
                x.SourcePost.Score,
                x.SourcePost.WordCount,
                IsProcessed = x.SourcePost.ProcessedPost != null,
                SourcePostStatus = x.SourcePost.Status,
                ProcessedPostId = x.SourcePost.ProcessedPost != null ? (Guid?)x.SourcePost.ProcessedPost.Id : null,
                TtsStatus = x.SourcePost.ProcessedPost != null ? (TtsStatus?)x.SourcePost.ProcessedPost.TtsStatus : null,
                VideoStatus = x.SourcePost.ProcessedPost != null ? (VideoStatus?)x.SourcePost.ProcessedPost.VideoStatus : null,
                NarratorGender = x.SourcePost.ProcessedPost != null ? (NarratorGender?)x.SourcePost.ProcessedPost.NarratorGender : null,
                IsPosted = x.SourcePost.ProcessedPost != null ? (bool?)x.SourcePost.ProcessedPost.IsPosted : null,
                x.CreatedUtc,
                VideoBlobContainer = x.SourcePost.ProcessedPost != null
                    && x.SourcePost.ProcessedPost.VideoStatus == VideoStatus.Generated
                    && x.SourcePost.ProcessedPost.VideoBlobPath != null
                    ? x.SourcePost.ProcessedPost.VideoBlobPath.ContainerName
                    : null,
                VideoBlobPath = x.SourcePost.ProcessedPost != null
                    && x.SourcePost.ProcessedPost.VideoStatus == VideoStatus.Generated
                    && x.SourcePost.ProcessedPost.VideoBlobPath != null
                    ? x.SourcePost.ProcessedPost.VideoBlobPath.AssetPath
                    : null
            })
            .ToListAsync(cancellationToken);

        var result = new List<ScheduledDayDto>(rows.Count);

        foreach (var row in rows)
        {
            var dto = new ScheduledDayDto
            {
                ScheduledPostId = row.ScheduledPostId,
                ScheduledDate = row.ScheduledDate,
                SourcePostId = row.SourcePostId,
                Title = row.Title,
                Community = row.Community,
                Score = row.Score,
                WordCount = row.WordCount,
                IsProcessed = row.IsProcessed,
                SourcePostStatus = row.SourcePostStatus,
                ProcessedPostId = row.ProcessedPostId,
                TtsStatus = row.TtsStatus,
                VideoStatus = row.VideoStatus,
                NarratorGender = row.NarratorGender,
                IsPosted = row.IsPosted,
                CreatedUtc = row.CreatedUtc
            };

            if (row.VideoBlobContainer != null && row.VideoBlobPath != null)
            {
                var sasUrl = await blobService.GetSasUrl(
                    row.VideoBlobContainer,
                    row.VideoBlobPath,
                    cancellationToken);
                dto.VideoSasUrl = sasUrl.ToString();
            }

            result.Add(dto);
        }

        return result;
    }
}
