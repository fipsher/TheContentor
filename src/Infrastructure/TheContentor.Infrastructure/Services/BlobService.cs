using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure.Interfaces;

namespace TheContentor.Infrastructure.Services;

public class BlobService(BlobServiceClient blobServiceClient) : IBlobService
{
    public async Task<BlobPath> UploadAsync(Stream stream, string containerName, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        await container.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var fileAndExt = fileName.Split('.');
        fileName = fileAndExt.Length > 1 
            ? $"{string.Join('.', fileAndExt[..^2])}-{Guid.NewGuid()}.{fileAndExt[^1]}" 
            : $"{fileName}-{Guid.NewGuid()}";
        
        var blob = container.GetBlobClient(fileName);
        
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        
        return new BlobPath
        {
            ContainerName = containerName,
            AssetPath = fileName
        };
    }

    public async Task<BlobPath> UploadToPathAsync(Stream stream, string containerName, string blobPath, string contentType, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        await container.SetAccessPolicyAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);

        var blob = container.GetBlobClient(blobPath);
        
        await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType }, cancellationToken: cancellationToken);
        
        return new BlobPath
        {
            ContainerName = containerName,
            AssetPath = blobPath
        };
    }

    public async Task<Uri> GetSasUrl(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var container = blobServiceClient.GetBlobContainerClient(containerName);
        var blob = container.GetBlobClient(blobName);
        
        var sas = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddHours(1),
            Protocol = SasProtocol.HttpsAndHttp
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(sas);
    }

    public async Task DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<Stream> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }
}
