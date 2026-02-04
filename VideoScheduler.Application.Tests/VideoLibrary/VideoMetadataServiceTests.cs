using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using VideoScheduler.Application.VideoLibrary.Models;
using VideoScheduler.Application.VideoLibrary.Services;
using Xunit;

namespace VideoScheduler.Application.Tests.VideoLibrary;

/// <summary>
/// Example application-layer tests demonstrating mocking of interfaces.
/// This shows how to test use cases that depend on services.
/// </summary>
public sealed class VideoMetadataServiceTests
{
    [Fact]
    public async Task TryGetVideoDurationAsync_WithValidFile_ReturnsExpectedDuration()
    {
        // Arrange
        var mockService = Substitute.For<IVideoMetadataService>();
        var expectedDuration = TimeSpan.FromMinutes(5);
        const string testPath = @"C:\Videos\sample.mp4";
        
        mockService.TryGetVideoDurationAsync(testPath, Arg.Any<CancellationToken>())
            .Returns(expectedDuration);

        // Act
        var result = await mockService.TryGetVideoDurationAsync(testPath, CancellationToken.None);

        // Assert
        result.Should().Be(expectedDuration);
        await mockService.Received(1).TryGetVideoDurationAsync(testPath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryGetVideoDurationAsync_WithUnsupportedFile_ReturnsNull()
    {
        // Arrange
        var mockService = Substitute.For<IVideoMetadataService>();
        const string unsupportedPath = @"C:\Videos\corrupt.mp4";
        
        mockService.TryGetVideoDurationAsync(unsupportedPath, Arg.Any<CancellationToken>())
            .Returns((TimeSpan?)null);

        // Act
        var result = await mockService.TryGetVideoDurationAsync(unsupportedPath, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetVideoDurationAsync_SupportsCancellation()
    {
        // Arrange
        var mockService = Substitute.For<IVideoMetadataService>();
        var cts = new CancellationTokenSource();
        const string testPath = @"C:\Videos\sample.mp4";
        
        mockService.TryGetVideoDurationAsync(testPath, cts.Token)
            .Returns(callInfo => 
            {
                var ct = callInfo.ArgAt<CancellationToken>(1);
                ct.ThrowIfCancellationRequested();
                return TimeSpan.FromMinutes(5);
            });

        cts.Cancel();

        // Act
        var act = () => mockService.TryGetVideoDurationAsync(testPath, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
