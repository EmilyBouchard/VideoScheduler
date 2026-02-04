---
applyTo: "VideoScheduler.Infrastructure/**/*.cs"
---

# Infrastructure Layer Instructions

## Purpose
The Infrastructure layer provides **concrete implementations** of Application interfaces, including file system access, media operations, and OS integration.

## Rules
- **Implement interfaces** defined in Application layer
- Handle **I/O errors gracefully** with try-catch blocks
- Use **Microsoft Media Foundation** for media operations (not ffmpeg)
- **Never expose Media Foundation types** outside Infrastructure - wrap them
- All file operations must be **async**
- **Fail gracefully** with placeholders/defaults for unsupported media formats
- Respect **CancellationToken** for long-running operations

## Media Foundation Requirements
- Initialize with `MFStartup` once per process (at app startup)
- Shutdown with `MFShutdown` at app exit
- Wrap all MF operations in try-catch to handle codec issues
- Use `IMFSourceReader` for metadata extraction
- Extract thumbnails using `IMFMediaSource` with frame sampling

## Patterns
- Use **SemaphoreSlim** to throttle concurrent I/O operations
- Wrap native interop with `using` statements for proper disposal
- Log errors but don't throw for unsupported formats - return nulls/defaults
- Use `IAsyncEnumerable` with `yield return` for streaming results

## Example
```csharp
public class VideoMetadataService : IVideoMetadataService
{
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);
    
    public async Task<VideoMetadataDto?> GetMetadataAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            // Media Foundation extraction code
            // Return null for unsupported formats
            return extractedMetadata;
        }
        catch (Exception ex)
        {
            // Log but don't throw - graceful degradation
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
```

## What NOT to do
- ❌ Don't introduce ffmpeg or external media tools without an ADR
- ❌ Don't expose Media Foundation types in public APIs
- ❌ Don't throw exceptions for unsupported formats - return nulls
- ❌ Don't block the calling thread with synchronous I/O
- ❌ Don't add WPF or UI dependencies
