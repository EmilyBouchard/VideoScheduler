using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VideoScheduler.Application.VideoLibrary.Models;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary;

public sealed class FileSystemVideoLibraryScanner : IVideoLibraryScanner
{
    private static readonly HashSet<String> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".mov", ".avi", ".wmv", ".m4v"
    };

    public async IAsyncEnumerable<FolderNodeDto> EnumerateFoldersAsync(
        string rootFolder,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var dir in SafeEnumerateDirectories(rootFolder))
        {
            ct.ThrowIfCancellationRequested();
            yield return new FolderNodeDto(dir, Path.GetFileName(dir));
            await Task.Yield();
        }
    }


    public async IAsyncEnumerable<VideoAssetDto> EnumerateVideosAsync(
        string rootFolder,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var file in SafeEnumerateFilesRecursive(rootFolder))
        {
            ct.ThrowIfCancellationRequested();
            
            var ext = Path.GetExtension(file);
            if (!VideoExtensions.Contains(ext))
                continue;
            
            FileInfo? info = null;
            try
            {
                info = new FileInfo(file);
            }
            catch
            {
                // TODO: Fail gracefully?
            }

            yield return new VideoAssetDto(
                FullPath: file,
                FileName: Path.GetFileName(file),
                SizeInBytes: info?.Exists == true ? info.Length : 0,
                LastWriteTimeUtc: info?.Exists == true
                    ? new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero)
                    : DateTimeOffset.MinValue
            );
            
            await Task.Yield();
        }
    }
    
    private static IEnumerable<string> SafeEnumerateDirectories(string rootFolder)
    {
        try
        {
            return Directory.EnumerateDirectories(rootFolder, "*", SearchOption.AllDirectories);
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateFilesRecursive(string rootFolder)
    {
        try
        {
            return Directory.EnumerateFiles(rootFolder, "*.*", SearchOption.AllDirectories);
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }
}