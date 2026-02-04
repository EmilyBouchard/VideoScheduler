using System.Threading;
using System.Threading.Tasks;

namespace VideoScheduler.Application.VideoLibrary.Services;

/// <summary>
/// Service for extracting thumbnail images from video files.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Attempts to extract a thumbnail from the specified video file.
    /// Returns null if extraction fails or the format is unsupported.
    /// </summary>
    /// <param name="filePath">Full path to the video file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Byte array containing the thumbnail image data (as PNG), or null if extraction failed.</returns>
    Task<byte[]?> TryExtractThumbnailAsync(string filePath, CancellationToken ct = default);
}
