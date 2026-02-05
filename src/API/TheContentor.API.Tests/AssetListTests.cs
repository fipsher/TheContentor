using Bunit;
using Xunit;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Components;
using TheContentor.Application.Features.Assets.Models;
using TheContentor.Application.Features.Assets.Queries;
using TheContentor.Application.Features.Assets.Commands;
using TheContentor.API.Components.Pages.Assets;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // Added this line

namespace TheContentor.API.Tests;

public class AssetListTests : Bunit.BunitContext
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<NavigationManager> _mockNavigationManager;

    public AssetListTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockNavigationManager = new Mock<NavigationManager>();

        // Register the mocked services with the TestContext
        Services.AddSingleton<IMediator>(_mockMediator.Object);
        Services.AddSingleton<NavigationManager>(_mockNavigationManager.Object);
    }

    [Fact]
    public void AssetList_RendersCorrectly_WithNoAssets()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAssetListQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(new List<AssetDto>()); // Return an empty list of assets

        // Act
        var cut = Render<AssetList>();

        // Assert
        cut.MarkupMatches(@"
            <div class=""d-flex justify-content-between align-items-center mb-4"">
                <h1>Asset Library</h1>
                <button class=""btn btn-primary"">
                    <i class=""bi bi-plus-lg""></i> Add New Asset
                </button>
                <button class=""btn btn-secondary"">
                    <i class=""bi bi-youtube""></i> Upload YouTube Asset
                </button>
            </div>
            <div class=""alert alert-info"">
                No assets found. Click ""Add New Asset"" to get started.
            </div>
        ");
    }

    [Fact]
    public void AssetList_RendersCorrectly_WithAssets()
    {
        // Arrange
        var assets = new List<AssetDto>
        {
            new AssetDto
            {
                Id = Guid.NewGuid(),
                FileName = "test_video_1.mp4",
                Duration = TimeSpan.FromSeconds(120),
                Tags = "tag1, tag2",
                IsActive = true,
                SasUri = new Uri("http://example.com/video1.mp4")
            },
            new AssetDto
            {
                Id = Guid.NewGuid(),
                FileName = "test_video_2.mp4",
                Duration = TimeSpan.FromSeconds(300),
                Tags = "tag3",
                IsActive = false,
                SasUri = new Uri("http://example.com/video2.mp4")
            }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAssetListQuery>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync(assets);

        // Act
        var cut = Render<AssetList>();

        // Assert
        cut.WaitForElement("table"); // Wait for the table to render, indicating assets are loaded

        cut.MarkupMatches($@"
            <div class=""d-flex justify-content-between align-items-center mb-4"">
                <h1>Asset Library</h1>
                <button class=""btn btn-primary"">
                    <i class=""bi bi-plus-lg""></i> Add New Asset
                </button>
                <button class=""btn btn-secondary"">
                    <i class=""bi bi-youtube""></i> Upload YouTube Asset
                </button>
            </div>
            <table class=""table table-hover"">
                <thead>
                    <tr>
                        <th>File Name</th>
                        <th>Duration</th>
                        <th>Tags</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td>test_video_1.mp4</td>
                        <td>02:00</td>
                        <td>
                            <span class=""badge bg-info text-dark me-1"">tag1</span>
                            <span class=""badge bg-info text-dark me-1"">tag2</span>
                        </td>
                        <td>
                            <span class=""badge bg-success"">Active</span>
                        </td>
                        <td>
                            <div class=""btn-group"">
                                <button class=""btn btn-sm btn-outline-primary"">
                                    <i class=""bi bi-play-circle""></i> View
                                </button>
                                <button class=""btn btn-sm btn-outline-warning"">
                                    <i class=""bi bi-eye-slash""></i> Deactivate
                                </button>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td>test_video_2.mp4</td>
                        <td>05:00</td>
                        <td>
                            <span class=""badge bg-info text-dark me-1"">tag3</span>
                        </td>
                        <td>
                            <span class=""badge bg-secondary"">Inactive</span>
                        </td>
                        <td>
                            <div class=""btn-group"">
                                <button class=""btn btn-sm btn-outline-primary"">
                                    <i class=""bi bi-play-circle""></i> View
                                </button>
                                <button class=""btn btn-sm btn-outline-success"">
                                    <i class=""bi bi-eye""></i> Activate
                                </button>
                            </div>
                        </td>
                    </tr>
                </tbody>
            </table>
        ");
    }
}
