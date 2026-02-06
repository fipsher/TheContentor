using Moq;
using TheContentor.Infrastructure.YouTube;
using YoutubeExplode;

namespace TheContentor.Infrastructure.Tests;

[TestFixture]
public class YouTubeServiceTests
{
    private Mock<YoutubeClient> _mockYoutubeClient;
    private YouTubeService _youTubeService;

    [SetUp]
    public void Setup()
    {
        _mockYoutubeClient = new Mock<YoutubeClient>();
        // Since YoutubeClient doesn't have a public parameterless constructor,
        // we can't directly mock it and pass it to YouTubeService's constructor easily without modifying YouTubeService.
        // For now, we'll create a new instance of YouTubeService for each test,
        // and acknowledge that direct mocking of YoutubeClient within YouTubeService's current structure is challenging.
        // A better approach would be to inject an IYoutubeClientWrapper into YouTubeService.
        _youTubeService = new YouTubeService();
    }

    [Test]
    public async Task IsValidYouTubeUrlAsync_WithValidUrl_ReturnsTrue()
    {
        // Arrange
        var validUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var result = await _youTubeService.IsValidYouTubeUrlAsync(validUrl);

        // Assert
        Assert.That(result, Is.True);
        
    }

    [Test]
    public async Task IsValidYouTubeUrlAsync_WithInvalidUrl_ReturnsFalse()
    {
        // Arrange
        var invalidUrl = "https://www.google.com";

        // Act
        var result = await _youTubeService.IsValidYouTubeUrlAsync(invalidUrl);

        // Assert
        Assert.That(result, Is.False);
    }

    // Note: Mocking YoutubeExplode directly is complex due to its internal structure and sealed classes.
    // The current YouTubeService instantiates YoutubeClient directly, making it hard to inject a mock.
    // For a real-world scenario, an adapter/wrapper around YoutubeClient would be created and injected.
    // The following tests for GetVideoMetadataAsync and DownloadVideoStreamAsync will be limited
    // or commented out due to this mocking challenge.
    // A potential way to test would be to use integration tests with a known YouTube video.
    // For now, these tests will serve as placeholders or simplified checks.

    [Test]
    public async Task GetVideoMetadataAsync_WithValidUrl_ReturnsMetadata()
    {
        // This is an integration test as we cannot easily mock YoutubeClient directly
        // without refactoring YouTubeService to accept an IYoutubeClientWrapper.
        // For demonstration, this test will use a real YouTube URL.
        // In a proper unit test setup, YoutubeClient would be mocked.

        // Arrange
        var validUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; // Known short public video

        // Act
        var metadata = await _youTubeService.GetVideoMetadataAsync(validUrl);

        // Assert
        Assert.That(metadata, Is.Not.Null);
        Assert.That(metadata?.Duration.TotalSeconds, Is.GreaterThan(0));
        Assert.That(metadata?.Width, Is.GreaterThan(0));
        Assert.That(metadata?.Height, Is.GreaterThan(0));
        Assert.That(metadata?.OriginalUrl, Is.Not.Null);
        Assert.That(metadata?.Title, Is.Not.Null);
    }

    [Test]
    public async Task GetVideoMetadataAsync_WithInvalidUrl_ReturnsNull()
    {
        // Arrange
        var invalidUrl = "https://www.google.com";

        // Act
        var metadata = await _youTubeService.GetVideoMetadataAsync(invalidUrl);

        // Assert
        Assert.That(metadata, Is.Null);
    }
    
    [Test]
    public async Task GetVideoMetadataAsync_WithNonExistentYouTubeVideo_ReturnsNull()
    {
        // Arrange
        var nonExistentVideoUrl = "https://www.youtube.com/watch?v=xxxxxxxxxxx"; // A plausible non-existent video ID

        // Act
        var metadata = await _youTubeService.GetVideoMetadataAsync(nonExistentVideoUrl);

        // Assert
        Assert.That(metadata, Is.Null);
    }

    [Test]
    public async Task DownloadVideoStreamAsync_WithValidUrl_ReturnsStream()
    {
        // This is an integration test. See comments in GetVideoMetadataAsync_WithValidUrl_ReturnsMetadata.
        // Arrange
        var validUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"; // Known short public video

        // Act
        await using var stream = await _youTubeService.DownloadVideoStreamAsync(validUrl);

        // Assert
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task DownloadVideoStreamAsync_WithInvalidUrl_ReturnsNull()
    {
        // Arrange
        var invalidUrl = "https://www.google.com";

        // Act
        var stream = await _youTubeService.DownloadVideoStreamAsync(invalidUrl);

        // Assert
        Assert.That(stream, Is.Null);
    }
    
    [Test]
    public async Task DownloadVideoStreamAsync_WithNonExistentYouTubeVideo_ReturnsNull()
    {
        // Arrange
        var nonExistentVideoUrl = "https://www.youtube.com/watch?v=xxxxxxxxxxx"; // A plausible non-existent video ID

        // Act
        var stream = await _youTubeService.DownloadVideoStreamAsync(nonExistentVideoUrl);

        // Assert
        Assert.That(stream, Is.Null);
    }
}
