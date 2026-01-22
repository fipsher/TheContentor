using MediatR;
using Microsoft.EntityFrameworkCore;
using TheContentor.Infrastructure;

namespace TheContentor.Application.Features.Assets.Commands;

public record UpdateAssetCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}

public class UpdateAssetCommandHandler(TheContentorDbContext context) : IRequestHandler<UpdateAssetCommand, bool>
{
    public async Task<bool> Handle(UpdateAssetCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Assets
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        entity.FileName = request.FileName;
        entity.BlobPath = request.LocalPath;
        entity.Tags = request.Tags;
        entity.Duration = request.Duration;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
