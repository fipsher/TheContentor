using Microsoft.AspNetCore.SignalR;

namespace TheContentor.API.Hubs;

/// <summary>Hub for real-time video generation progress updates.</summary>
public class VideoGenerationHub : Hub
{
    /// <summary>Adds caller to a group for the given processed post.</summary>
    public async Task JoinPostGroup(Guid processedPostId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, processedPostId.ToString());
    }

    /// <summary>Removes caller from a processed post group.</summary>
    public async Task LeavePostGroup(Guid processedPostId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, processedPostId.ToString());
    }

    /// <summary>Adds caller to a group for AI processing updates on a source post.</summary>
    public async Task JoinSourcePostGroup(Guid sourcePostId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sourcepost-{sourcePostId}");
    }

    /// <summary>Removes caller from a source post AI processing group.</summary>
    public async Task LeaveSourcePostGroup(Guid sourcePostId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sourcepost-{sourcePostId}");
    }

    /// <summary>Adds caller to a group for generate-week progress updates.</summary>
    public async Task JoinGenerateWeekGroup(string weekStart)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"generate-week-{weekStart}");
    }

    /// <summary>Removes caller from a generate-week progress group.</summary>
    public async Task LeaveGenerateWeekGroup(string weekStart)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"generate-week-{weekStart}");
    }
}
