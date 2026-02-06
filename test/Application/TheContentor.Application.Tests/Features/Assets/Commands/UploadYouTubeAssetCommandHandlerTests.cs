using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TheContentor.Application.Features.Assets.Commands;
using TheContentor.Infrastructure;
using TheContentor.Infrastructure.Interfaces;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using Microsoft.EntityFrameworkCore; // Added this using directive

namespace TheContentor.Application.Tests.Features.Assets.Commands;

public class UploadYouTubeAssetCommandHandlerTests
{
    private readonly Mock<TheContentorDbContext> _mockContext;
    private readonly Mock<IYouTubeService> _mockYouTubeService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly UploadYouTubeAssetCommandHandler _handler;

    public UploadYouTubeAssetCommandHandlerTests()
    {
        // Mock DbContext using an in-memory database for testing
        var options = new DbContextOptionsBuilder<TheContentorDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique database for each test
            .Options;
        _mockContext = new Mock<TheContentorDbContext>(options);
        _mockContext.Setup(c => c.Assets).Returns(new Mock<DbSet<Asset>>().Object);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockYouTubeService = new Mock<IYouTubeService>();
        _mockBlobService = new Mock<IBlobService>();

        _handler = new UploadYouTubeAssetCommandHandler(
            _mockContext.Object,
            _mockYouTubeService.Object,
            _mockBlobService.Object);
    }

    [Fact]
    public async Task Handle_ValidYouTubeUrl_ShouldUploadAssetAndReturnId()
    {
        // Arrange
        var youtubeUrl = "https://www.youtube.com/watch?v=testVideoId";
        var expectedAssetId = Guid.NewGuid();
        var expectedBlobPath = new BlobPath { ContainerName = "assets", AssetPath = "youtube_videos/test.mp4" }; // Corrected instantiation

        _mockYouTubeService.Setup(s => s.IsValidYouTubeUrlAsync(youtubeUrl)).ReturnsAsync(true);
        _mockYouTubeService.Setup(s => s.GetVideoMetadataAsync(youtubeUrl))
            .ReturnsAsync((TimeSpan.FromMinutes(2), youtubeUrl, "Test YouTube Video")); // Updated tuple
        
        // Create a dummy temporary file for the mock to return
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllBytes(tempFilePath, new byte[] { 1, 2, 3 });
        _mockYouTubeService.Setup(s => s.DownloadVideoStreamAsync(youtubeUrl))
            .ReturnsAsync(new FileInfo(tempFilePath)); // Returns FileInfo?

        _mockBlobService.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBlobPath);

        _mockContext.Setup(c => c.Assets.Add(It.IsAny<Asset>()))
            .Callback<Asset>(asset =>
            {
                // Assign a dummy ID for the test if needed for return value
                asset.Id = expectedAssetId;
            });
        
        var command = new UploadYouTubeAssetCommand(youtubeUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(expectedAssetId);
        _mockYouTubeService.Verify(s => s.IsValidYouTubeUrlAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.GetVideoMetadataAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.DownloadVideoStreamAsync(youtubeUrl), Times.Once);
        _mockBlobService.Verify(s => s.UploadAsync(
                It.IsAny<Stream>(),
                "assets", // Expected container
                It.Is<string>(fn => fn.StartsWith("youtube_videos/")), // Expected file name prefix
                "video/mp4", // Expected content type
                It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.Assets.Add(It.Is<Asset>(a =>
            a.OriginalUrl == youtubeUrl &&
            a.Title == "Test YouTube Video" &&
            a.Duration == TimeSpan.FromMinutes(2) &&
            a.Type == AssetType.YouTube &&
            a.BlobPath.ContainerName == expectedBlobPath.ContainerName && // Verify BlobPath properties
            a.BlobPath.AssetPath == expectedBlobPath.AssetPath)), Times.Once); // Removed Width, Height, UploadDate assertions
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidYouTubeUrl_ShouldThrowArgumentException()
    {
        // Arrange
        var youtubeUrl = "invalid-youtube-url";
        _mockYouTubeService.Setup(s => s.IsValidYouTubeUrlAsync(youtubeUrl)).ReturnsAsync(false);
        var command = new UploadYouTubeAssetCommand(youtubeUrl);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid YouTube URL provided.*");

        _mockYouTubeService.Verify(s => s.IsValidYouTubeUrlAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.GetVideoMetadataAsync(It.IsAny<string>()), Times.Never);
        _mockYouTubeService.Verify(s => s.DownloadVideoStreamAsync(It.IsAny<string>()), Times.Never);
        _mockBlobService.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockContext.Verify(c => c.Assets.Add(It.IsAny<Asset>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_GetVideoMetadataFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var youtubeUrl = "https://www.youtube.com/watch?v=testVideoId";
        _mockYouTubeService.Setup(s => s.IsValidYouTubeUrlAsync(youtubeUrl)).ReturnsAsync(true);
        _mockYouTubeService.Setup(s => s.GetVideoMetadataAsync(youtubeUrl)).ReturnsAsync((
            (TimeSpan Duration, string OriginalUrl, string Title)?)null); // Simulate failure, updated tuple type
        var command = new UploadYouTubeAssetCommand(youtubeUrl);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Could not retrieve video metadata from the provided URL.");

        _mockYouTubeService.Verify(s => s.IsValidYouTubeUrlAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.GetVideoMetadataAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.DownloadVideoStreamAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DownloadVideoStreamFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var youtubeUrl = "https://www.youtube.com/watch?v=testVideoId";
        _mockYouTubeService.Setup(s => s.IsValidYouTubeUrlAsync(youtubeUrl)).ReturnsAsync(true);
        _mockYouTubeService.Setup(s => s.GetVideoMetadataAsync(youtubeUrl))
            .ReturnsAsync((TimeSpan.FromMinutes(2), youtubeUrl, "Test YouTube Video")); // Updated tuple
        _mockYouTubeService.Setup(s => s.DownloadVideoStreamAsync(youtubeUrl)).ReturnsAsync((FileInfo?)null); // Simulate failure, returns FileInfo?
        var command = new UploadYouTubeAssetCommand(youtubeUrl);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Could not download video stream from the provided URL.");

        _mockYouTubeService.Verify(s => s.IsValidYouTubeUrlAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.GetVideoMetadataAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.DownloadVideoStreamAsync(youtubeUrl), Times.Once);
        _mockBlobService.Verify(s => s.UploadAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BlobUploadFails_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var youtubeUrl = "https://www.youtube.com/watch?v=testVideoId";
        _mockYouTubeService.Setup(s => s.IsValidYouTubeUrlAsync(youtubeUrl)).ReturnsAsync(true);
        _mockYouTubeService.Setup(s => s.GetVideoMetadataAsync(youtubeUrl))
            .ReturnsAsync((TimeSpan.FromMinutes(2), youtubeUrl, "Test YouTube Video")); // Updated tuple
        
        // Create a dummy temporary file for the mock to return
        var tempFilePath = Path.GetTempFileName();
        File.WriteAllBytes(tempFilePath, new byte[] { 1, 2, 3 });
        _mockYouTubeService.Setup(s => s.DownloadVideoStreamAsync(youtubeUrl))
            .ReturnsAsync(new FileInfo(tempFilePath)); // Returns FileInfo?

        _mockBlobService.Setup(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((BlobPath?)null); // Simulate failure
        var command = new UploadYouTubeAssetCommand(youtubeUrl);

        // Act & Assert
        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to upload video to blob storage.");

        _mockYouTubeService.Verify(s => s.IsValidYouTubeUrlAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.GetVideoMetadataAsync(youtubeUrl), Times.Once);
        _mockYouTubeService.Verify(s => s.DownloadVideoStreamAsync(youtubeUrl), Times.Once);
        _mockBlobService.Verify(s => s.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.Assets.Add(It.IsAny<Asset>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}