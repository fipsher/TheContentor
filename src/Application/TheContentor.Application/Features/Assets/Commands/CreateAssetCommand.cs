using MediatR;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.Assets.Commands;

public record CreateAssetCommand : IRequest<Guid>
{
    public string FileName { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public Stream? FileStream { get; set; }
    public string? ContentType { get; set; }
}

public class CreateAssetCommandHandler(
    TheContentorDbContext context,
    IBlobService blobService) : IRequestHandler<CreateAssetCommand, Guid>
{
    public async Task<Guid> Handle(CreateAssetCommand request, CancellationToken cancellationToken)
    {
        string localPath;
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

            localPath = await blobService.UploadAsync(uploadStream, request.FileName,
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
            FileName = request.FileName,
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