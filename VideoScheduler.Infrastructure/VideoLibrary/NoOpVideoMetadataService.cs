using System;
using System.Threading;
using System.Threading.Tasks;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary;

public sealed class NoOpVideoMetadataService : IVideoMetadataService
{
    public Task<TimeSpan?> TryGetVideoDurationAsync(string filePath, CancellationToken ct = default)
        => Task.FromResult<TimeSpan?>(null);
}