using TheContentor.Domain.Entities;

namespace TheContentor.Infrastructure.Interfaces;

public interface IBlobService
{
    Task<BlobPath?> UploadAsync(Stream stream, string containerName, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Uri> GetSasUrl(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
