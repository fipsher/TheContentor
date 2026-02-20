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
}
