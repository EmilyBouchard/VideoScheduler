using System;
using FluentAssertions;
using Xunit;

namespace VideoScheduler.Domain.Tests;

/// <summary>
/// Unit tests for the VideoAsset domain entity.
/// Demonstrates domain-level testing without dependencies.
/// </summary>
public sealed class VideoAssetTests
{
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        const string filePath = @"C:\Videos\sample.mp4";
        const string fileName = "sample.mp4";
        const long sizeInBytes = 1024000;

        // Act
        var asset = new VideoAsset(filePath, fileName, sizeInBytes);

        // Assert
        asset.FilePath.Should().Be(filePath);
        asset.FileName.Should().Be(fileName);
        asset.SizeInBytes.Should().Be(sizeInBytes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFilePath_ThrowsArgumentException(string? invalidPath)
    {
        // Arrange & Act
        var act = () => new VideoAsset(invalidPath!, "file.mp4", 1024);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidFileName_ThrowsArgumentException(string? invalidName)
    {
        // Arrange & Act
        var act = () => new VideoAsset(@"C:\Videos\test.mp4", invalidName!, 1024);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("fileName");
    }

    [Fact]
    public void Constructor_WithNegativeSize_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new VideoAsset(@"C:\Videos\test.mp4", "test.mp4", -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sizeInBytes");
    }

    [Theory]
    [InlineData(512, "512 bytes")]
    [InlineData(1024, "1.00 KB")]
    [InlineData(2048, "2.00 KB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(1572864, "1.50 MB")]
    [InlineData(1073741824, "1.00 GB")]
    [InlineData(2147483648, "2.00 GB")]
    public void GetSizeDisplayString_FormatsCorrectly(long bytes, string expected)
    {
        // Arrange
        var asset = new VideoAsset(@"C:\test.mp4", "test.mp4", bytes);

        // Act
        var result = asset.GetSizeDisplayString();

        // Assert
        result.Should().Be(expected);
    }
}
