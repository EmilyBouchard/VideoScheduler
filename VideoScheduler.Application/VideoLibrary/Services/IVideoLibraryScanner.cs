using System.Collections.Generic;
using System.Threading;
using VideoScheduler.Application.VideoLibrary.Models;

namespace VideoScheduler.Application.VideoLibrary.Services;

public interface IVideoLibraryScanner
{
    IAsyncEnumerable<FolderNodeDto> EnumerateFoldersAsync(
        string rootFolder,
        CancellationToken ct = default
    );

    IAsyncEnumerable<VideoAssetDto> EnumerateVideosAsync(
        string rootFolder,
        CancellationToken ct = default
    );
}
