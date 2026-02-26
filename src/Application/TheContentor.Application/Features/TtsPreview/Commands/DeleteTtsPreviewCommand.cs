using MediatR;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Application.Features.TtsPreview.Commands;

/// <summary>Deletes a TTS preview audio file from storage.</summary>
public record DeleteTtsPreviewCommand(string AudioPath) : IRequest;

/// <summary>Resolves the container and file path from <see cref="DeleteTtsPreviewCommand.AudioPath"/> and deletes the file.</summary>
public class DeleteTtsPreviewCommandHandler(IBlobService blobService) : IRequestHandler<DeleteTtsPreviewCommand>
{
    /// <summary>Splits the audio path on '/' to extract the container name and file path, then deletes the blob.</summary>
    public async Task Handle(DeleteTtsPreviewCommand request, CancellationToken cancellationToken)
    {
        var slashIndex = request.AudioPath.IndexOf('/');
        if (slashIndex < 0)
        {
            throw new ArgumentException($"AudioPath '{request.AudioPath}' does not contain a '/' separator.");
        }

        var containerName = request.AudioPath[..slashIndex];
        var filePath = request.AudioPath[(slashIndex + 1)..];

        await blobService.DeleteAsync(containerName, filePath, cancellationToken);
    }
}
