using TheContentor.Domain.Common;

namespace TheContentor.Domain.Entities;

public class PostMetricSnapshot : BaseEntity
{
    public Guid SourcePostId { get; set; }
    public SourcePost SourcePost { get; set; } = null!;

    public DateTimeOffset CapturedUtc { get; set; }

    public int Score { get; set; }
    public int CommentCount { get; set; }
    public double? UpvoteRatio { get; set; }
}