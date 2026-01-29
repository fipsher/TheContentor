using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Commands;

/// <summary>Creates a new asset and uploads its stream.</summary>
public record CreateAssetCommand : IRequest<Guid>
{
    /// <summary>User-facing asset name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Tag string for filtering.</summary>
    public string Tags { get; set; } = string.Empty;
    /// <summary>Stream containing the asset file.</summary>
    public Stream? FileStream { get; set; }
    /// <summary>Content type for the uploaded file.</summary>
    public string? ContentType { get; set; }
}

/// <summary>Handles asset uploads and persistence.</summary>
public class CreateAssetCommandHandler(
    TheContentorDbContext context,
    IBlobService blobService) : IRequestHandler<CreateAssetCommand, Guid>
{
    /// <summary>Uploads the asset and stores metadata.</summary>
    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        BlobPath localPath;
        var duration = TimeSpan.Zero;

        if (request.FileStream == null)
        {
            throw new ArgumentNullException(nameof(request.FileStream));
        }

        // We need to copy the stream if it's not seekable, because GetDurationAsync consumes it
        // and blobService.UploadAsync needs it from the start.
        var uploadStream = request.FileStream;
        var shouldDisposeUploadStream = false;

        if (!request.FileStream.CanSeek)
        {
            var ms = new MemoryStream();
            await request.FileStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            uploadStream = ms;
            shouldDisposeUploadStream = true;
        }

        try
        {
            if (uploadStream.CanSeek)
            {
                uploadStream.Position = 0;
            }

            localPath = await blobService.UploadAsync(uploadStream, "assets", request.Name,
                request.ContentType ?? "video/mp4", cancellationToken);
        }
        finally
        {
            if (shouldDisposeUploadStream)
            {
                await uploadStream.DisposeAsync();
            }
        }

        var entity = new Asset
        {
            Name = request.Name,
            BlobPath = localPath,
            Tags = request.Tags,
            Duration = duration,
            IsActive = true
        };

        context.Assets.Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
