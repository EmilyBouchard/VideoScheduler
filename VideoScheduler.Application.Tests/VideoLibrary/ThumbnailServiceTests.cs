using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using VideoScheduler.Application.VideoLibrary.Services;
using Xunit;

namespace VideoScheduler.Application.Tests.VideoLibrary;

/// <summary>
/// Tests for the IThumbnailService interface demonstrating mocking.
/// </summary>
public sealed class ThumbnailServiceTests
{
    [Fact]
    public async Task TryExtractThumbnailAsync_WithValidFile_ReturnsBytes()
    {
        // Arrange
        var mockService = Substitute.For<IThumbnailService>();
        var expectedBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        const string testPath = @"C:\Videos\sample.mp4";
        
        mockService.TryExtractThumbnailAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(expectedBytes);

        // Act
        var result = await mockService.TryExtractThumbnailAsync(testPath, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(expectedBytes);
        await mockService.Received(1).TryExtractThumbnailAsync(testPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryExtractThumbnailAsync_WithUnsupportedFile_ReturnsNull()
    {
        // Arrange
        var mockService = Substitute.For<IThumbnailService>();
        const string unsupportedPath = @"C:\Videos\corrupt.mp4";
        
        mockService.TryExtractThumbnailAsync(unsupportedPath, Arg.Any<CancellationToken>())
            .Returns((byte[]?)null);

        // Act
        var result = await mockService.TryExtractThumbnailAsync(unsupportedPath, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryExtractThumbnailAsync_SupportsCancellation()
    {
        // Arrange
        var mockService = Substitute.For<IThumbnailService>();
        var cts = new CancellationTokenSource();
        const string testPath = @"C:\Videos\sample.mp4";
        
        mockService.TryExtractThumbnailAsync(testPath, cts.Token)
            .Returns(callInfo => 
            {
                var ct = callInfo.ArgAt<CancellationToken>(1);
                ct.ThrowIfCancellationRequested();
                return new byte[] { 0x89, 0x50, 0x4E, 0x47 };
            });

        cts.Cancel();

        // Act
        var act = () => mockService.TryExtractThumbnailAsync(testPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
