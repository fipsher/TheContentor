using Bunit;
using Xunit;
using Moq;
using MediatR;
using Microsoft.AspNetCore.Components;
using TheContentor.API.Components.Pages.Assets;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Bunit.Asserting;

namespace TheContentor.API.Tests;

public class YouTubeUploadModalTests : Bunit.BunitContext
{
    private readonly Mock<IMediator> _mockMediator; // Though not used directly in modal, might be passed through
    private readonly Mock<NavigationManager> _mockNavigationManager; // Not used directly

    public YouTubeUploadModalTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockNavigationManager = new Mock<NavigationManager>();

        Services.AddSingleton<IMediator>(_mockMediator.Object);
        Services.AddSingleton<NavigationManager>(_mockNavigationManager.Object);
    }

    [Fact]
    public void Modal_RendersCorrectly_WhenShown()
    {
        // Arrange
        var cut = Render<YouTubeUploadModal>(
            parameters => parameters.Add(p => p.ShowModal, true)
                                    .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
                                    .Add(p => p.OnUpload, EventCallback.Factory.Create<string>(this, _ => { })));

        // Assert
        // Check if the modal dialog is present and visible
        cut.MarkupMatches(@"
            <div class=""modal fade show d-block"" tabindex=""-1"" role=""dialog"" style=""background-color: rgba(0,0,0,0.5);"">
                <div class=""modal-dialog modal-dialog-centered"" role=""document"">
                    <div class=""modal-content"">
                        <div class=""modal-header"">
                            <h5 class=""modal-title"">Upload YouTube Asset</h5>
                            <button type=""button"" class=""btn-close""></button>
                        </div>
                        <div class=""modal-body"">
                            <div class=""mb-3"">
                                <label for=""youTubeUrlInput"" class=""form-label"">YouTube Video URL</label>
                                <input type=""text"" class=""form-control"" id=""youTubeUrlInput"" placeholder=""Enter YouTube URL here"" value="""">
                            </div>
                        </div>
                        <div class=""modal-footer"">
                            <button type=""button"" class=""btn btn-secondary"">Cancel</button>
                            <button type=""button"" class=""btn btn-primary"" disabled="""">Upload</button>
                        </div>
                    </div>
                </div>
            </div>
        ");
    }

    [Fact]
    public void Modal_ShowsValidationError_ForInvalidUrl()
    {
        // Arrange
        var cut = Render<YouTubeUploadModal>(
            parameters => parameters.Add(p => p.ShowModal, true)
                                    .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
                                    .Add(p => p.OnUpload, EventCallback.Factory.Create<string>(this, _ => { })));

        // Act - Simulate typing an invalid URL and blurring
        cut.Find("#youTubeUrlInput").Input("invalid-url");
        cut.Find("#youTubeUrlInput").Blur(); // Trigger validation

        // Assert
        cut.Find(".text-danger").MarkupMatches(@"<div class=""text-danger mt-1"">Invalid YouTube URL format.</div>");
        cut.Find(".btn-primary").HasAttribute("disabled");
    }

    [Fact]
    public void Modal_EnablesUploadButton_ForValidUrl()
    {
        // Arrange
        var cut = Render<YouTubeUploadModal>(
            parameters => parameters.Add(p => p.ShowModal, true)
                                    .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { }))
                                    .Add(p => p.OnUpload, EventCallback.Factory.Create<string>(this, _ => { })));

        // Act - Simulate typing a valid URL
        cut.Find("#youTubeUrlInput").Input("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        cut.Find("#youTubeUrlInput").Blur(); // Trigger validation

        // Assert
        cut.Find("div.mb-3").MarkupMatches(@"
            <div class=""mb-3"">
                <label for=""youTubeUrlInput"" class=""form-label"">YouTube Video URL</label>
                <input type=""text"" class=""form-control"" id=""youTubeUrlInput"" placeholder=""Enter YouTube URL here"" value=""https://www.youtube.com/watch?v=dQw4w9WgXcQ"">
            </div>
        "); // Ensure error message is not present
        Assert.False(cut.Find(".btn-primary").HasAttribute("disabled")); // Button should NOT be disabled
    }
}
