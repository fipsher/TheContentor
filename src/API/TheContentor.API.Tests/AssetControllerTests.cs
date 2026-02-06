using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using TheContentor.API.Controllers;
using TheContentor.API.Models;
using TheContentor.Application.Features.Assets.Commands;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http; // For StatusCodes

namespace TheContentor.API.Tests;

public class AssetControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly AssetController _controller;

    public AssetControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new AssetController(_mockMediator.Object);
    }

    [Fact]
    public async Task UploadYouTubeAsset_ValidUrl_ReturnsCreatedAtAction()
    {
        // Arrange
        var youTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var expectedAssetId = Guid.NewGuid();
        var model = new YouTubeAssetUploadModel { YouTubeUrl = youTubeUrl };

        _mockMediator.Setup(m => m.Send(It.IsAny<UploadYouTubeAssetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAssetId);

        // Act
        var result = await _controller.UploadYouTubeAsset(model);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdAtActionResult = result.Result.As<CreatedAtActionResult>();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        createdAtActionResult.ActionName.Should().NotBeNull().And.Be(nameof(AssetController.GetById));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        createdAtActionResult.RouteValues.Should().NotBeNull();
        createdAtActionResult.RouteValues!["id"].Should().Be(expectedAssetId);
        createdAtActionResult.Value.Should().Be(expectedAssetId);

        _mockMediator.Verify(m => m.Send(
            It.Is<UploadYouTubeAssetCommand>(cmd => cmd.YouTubeUrl == youTubeUrl),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadYouTubeAsset_InvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var youTubeUrl = "invalid-url";
        var model = new YouTubeAssetUploadModel { YouTubeUrl = youTubeUrl };

        _mockMediator.Setup(m => m.Send(It.IsAny<UploadYouTubeAssetCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid YouTube URL provided."));

        // Act
        var result = await _controller.UploadYouTubeAsset(model);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result.Result.As<BadRequestObjectResult>();
        badRequestResult.Value.Should().Be("Invalid YouTube URL provided.");

        _mockMediator.Verify(m => m.Send(
            It.Is<UploadYouTubeAssetCommand>(cmd => cmd.YouTubeUrl == youTubeUrl),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadYouTubeAsset_BackendFailure_ReturnsInternalServerError()
    {
        // Arrange
        var youTubeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";
        var model = new YouTubeAssetUploadModel { YouTubeUrl = youTubeUrl };

        _mockMediator.Setup(m => m.Send(It.IsAny<UploadYouTubeAssetCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Could not download video stream."));

        // Act
        var result = await _controller.UploadYouTubeAsset(model);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result.As<ObjectResult>();
        objectResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        objectResult.Value.Should().Be("Could not download video stream.");

        _mockMediator.Verify(m => m.Send(
            It.Is<UploadYouTubeAssetCommand>(cmd => cmd.YouTubeUrl == youTubeUrl),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
