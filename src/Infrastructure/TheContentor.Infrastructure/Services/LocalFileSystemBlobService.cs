using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheContentor.Domain.Entities;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Infrastructure.Options;

namespace TheContentor.Infrastructure.Services;

/// <summary>Local file system implementation of blob storage.</summary>
public class LocalFileSystemBlobService(
    IOptions<LocalStorageOptions> options,
    ILogger<LocalFileSystemBlobService> logger) : IBlobService
{
    private readonly string _basePath = options.Value.BasePath;

    /// <summary>Uploads a stream to the local file system.</summary>
    public async Task<BlobPath?> UploadAsync(
        Stream stream,
        string containerName,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidatePath(containerName, nameof(containerName));
            ValidatePath(fileName, nameof(fileName));

            var containerDir = Path.Combine(_basePath, containerName);
            Directory.CreateDirectory(containerDir);

            // Replicate the GUID-suffix filename logic from the original BlobService
            var fileAndExt = fileName.Split('.');
            fileName = fileAndExt.Length > 1
                ? $"{string.Join('.', fileAndExt[..^2])}-{Guid.NewGuid()}.{fileAndExt[^1]}"
                : $"{fileName}-{Guid.NewGuid()}";

            // Handle nested paths (e.g. youtube_videos/{guid}.mp4)
            var fullPath = Path.Combine(containerDir, fileName);
            var parentDir = Path.GetDirectoryName(fullPath);
            if (parentDir != null)
            {
                Directory.CreateDirectory(parentDir);
            }

            await using var fileStream = new FileStream(
                fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fileStream, cancellationToken);

            return new BlobPath
            {
                ContainerName = containerName,
                AssetPath = fileName
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file {FileName} to container {Container}", fileName, containerName);
            return null;
        }
    }

    /// <summary>Returns a relative download URL via the static file middleware.</summary>
    public Task<Uri> GetSasUrl(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        var encodedContainer = Uri.EscapeDataString(containerName);
        var encodedBlob = Uri.EscapeDataString(blobName);
        var url = $"/storage/{encodedContainer}/{encodedBlob}";
        return Task.FromResult(new Uri(url, UriKind.Relative));
    }

    /// <summary>Deletes a file from local storage.</summary>
    public Task DeleteAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(containerName, nameof(containerName));
        ValidatePath(blobName, nameof(blobName));

        var fullPath = Path.Combine(_basePath, containerName, blobName);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    /// <summary>Returns a read stream for the requested file.</summary>
    public Task<Stream> DownloadAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default)
    {
        ValidatePath(containerName, nameof(containerName));
        ValidatePath(blobName, nameof(blobName));

        var fullPath = Path.Combine(_basePath, containerName, blobName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Blob not found: {containerName}/{blobName}", fullPath);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    private static void ValidatePath(string value, string paramName)
    {
        if (value.Contains("..") || Path.IsPathRooted(value))
        {
            throw new ArgumentException("Path traversal is not allowed.", paramName);
        }
    }
}
