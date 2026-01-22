using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Infrastructure.Services;

public class BlobService(BlobServiceClient blobServiceClient) : IBlobService
{
    private readonly BlobContainerClient _blobContainerClient = blobServiceClient.GetBlobContainerClient("assets");

    public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await _blobContainerClient.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        
        var blobClient = _blobContainerClient.GetBlobClient(fileName);
        
        await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        
        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string GetBlobUrl(string blobName)
    {
        return _blobContainerClient.GetBlobClient(blobName).Uri.ToString();
    }
}
