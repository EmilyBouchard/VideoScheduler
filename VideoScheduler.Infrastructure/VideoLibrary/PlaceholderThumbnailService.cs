using System.Threading;
using System.Threading.Tasks;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary;

/// <summary>
/// Placeholder thumbnail service that returns null.
/// TODO: Implement using Media Foundation for actual thumbnail extraction.
/// </summary>
public sealed class PlaceholderThumbnailService : IThumbnailService
{
    public Task<byte[]?> TryExtractThumbnailAsync(string filePath, CancellationToken ct = default)
    {
        // Placeholder - returns null for now
        // Will be implemented with Media Foundation
        return Task.FromResult<byte[]?>(null);
    }
}
