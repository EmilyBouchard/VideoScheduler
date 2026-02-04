using System;
using System.Threading;
using System.Threading.Tasks;

namespace VideoScheduler.Application.VideoLibrary.Services;

public interface IVideoMetadataService
{
    Task<TimeSpan?> TryGetVideoDurationAsync(string filePath, CancellationToken ct = default);
}
