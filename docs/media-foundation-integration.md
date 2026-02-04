# Media Foundation Integration Guide

## Overview
This document describes the Microsoft Media Foundation integration for video metadata extraction and thumbnail generation in VideoScheduler.

## Architecture

### Lifecycle Management
Media Foundation requires proper initialization and shutdown:

```csharp
// App.xaml.cs - Startup
_mediaFoundationManager = new MediaFoundationManager();
_mediaFoundationManager.Initialize(); // Calls MFStartup

// App.xaml.cs - Shutdown
_mediaFoundationManager.Dispose(); // Calls MFShutdown
```

### Services Implemented

#### 1. MediaFoundationManager
**Purpose**: Manages Media Foundation lifetime  
**Location**: `Infrastructure/VideoLibrary/MediaFoundation/`

**Key Methods**:
- `Initialize()` - Calls `MFStartup()` once at app startup
- `Shutdown()` - Calls `MFShutdown()` at app exit
- `Dispose()` - Ensures proper cleanup

**P/Invoke APIs Used**:
```csharp
[DllImport("Mfplat.dll")]
private static extern int MFStartup(uint version, uint dwFlags);

[DllImport("Mfplat.dll")]
private static extern int MFShutdown();
```

#### 2. MediaFoundationMetadataService
**Purpose**: Extracts video metadata (duration, codecs, etc.)  
**Interface**: `IVideoMetadataService`  
**Returns**: `TimeSpan?` for duration, null for unsupported formats

**Implementation Details**:
```csharp
public async Task<TimeSpan?> TryGetVideoDurationAsync(string filePath, CancellationToken ct)
{
    // 1. Create source resolver
    MFCreateSourceResolver(out sourceResolver);
    
    // 2. Create media source from file URL
    IMFSourceResolver_CreateObjectFromURL(sourceResolver, filePath, ...);
    
    // 3. Get presentation descriptor
    IMFMediaSource_CreatePresentationDescriptor(mediaSource, out presentationDescriptor);
    
    // 4. Read MF_PD_DURATION attribute
    IMFPresentationDescriptor_GetUINT64(presentationDescriptor, ref durationGuid, out duration);
    
    // 5. Convert from 100-nanosecond units to TimeSpan
    return TimeSpan.FromTicks(duration100ns / 10);
}
```

**P/Invoke APIs Used**:
- `MFCreateSourceResolver` (Mfplat.dll)
- `IMFSourceResolver::CreateObjectFromURL` (Mf.dll)
- `IMFMediaSource::CreatePresentationDescriptor` (Mf.dll)
- `IMFPresentationDescriptor::GetUINT64` (Mf.dll)

**Error Handling**:
- Returns `null` for unsupported formats
- Returns `null` on any COM failure
- Properly releases all COM objects via `Marshal.Release()`
- Respects cancellation tokens

#### 3. MediaFoundationThumbnailService
**Purpose**: Extracts thumbnail frames from videos  
**Interface**: `IThumbnailService`  
**Returns**: `byte[]?` (PNG-encoded image), null for unsupported formats

**Current Status**: 🚧 Partial Implementation
- ✅ Opens video file via Media Foundation
- ✅ Creates source reader
- ✅ Seeks to 1-second position (skips black intro frames)
- ✅ Reads a video sample (frame)
- ✅ Locks media buffer and extracts raw frame data
- ⏳ TODO: Convert raw frame to RGB format
- ⏳ TODO: Create bitmap from RGB data
- ⏳ TODO: Encode as PNG and return bytes

**Implementation Details**:
```csharp
public async Task<byte[]?> TryExtractThumbnailAsync(string filePath, CancellationToken ct)
{
    // 1. Create source reader from file
    MFCreateSourceReaderFromMediaSource(mediaSource, ..., out sourceReader);
    
    // 2. Select first video stream
    IMFSourceReader_SetStreamSelection(sourceReader, FIRST_VIDEO_STREAM, true);
    
    // 3. Seek to 1 second (skip black frames)
    IMFSourceReader_SetCurrentPosition(sourceReader, ..., 10000000);
    
    // 4. Read a sample (video frame)
    IMFSourceReader_ReadSample(sourceReader, ..., out sample);
    
    // 5. Get contiguous buffer from sample
    IMFSample_ConvertToContiguousBuffer(sample, out mediaBuffer);
    
    // 6. Lock buffer and copy data
    IMFMediaBuffer_Lock(mediaBuffer, out bufferData, ...);
    Marshal.Copy(bufferData, frameData, 0, length);
    
    // 7. TODO: Convert to PNG
    return null; // Placeholder until PNG conversion implemented
}
```

**P/Invoke APIs Used**:
- `MFCreateSourceReaderFromMediaSource` (Mfreadwrite.dll)
- `IMFSourceReader::SetStreamSelection` (Mfreadwrite.dll)
- `IMFSourceReader::SetCurrentPosition` (Mfreadwrite.dll)
- `IMFSourceReader::ReadSample` (Mfreadwrite.dll)
- `IMFSample::ConvertToContiguousBuffer` (Mfplat.dll)
- `IMFMediaBuffer::Lock` (Mfplat.dll)
- `IMFMediaBuffer::Unlock` (Mfplat.dll)

## Performance Optimizations

### Throttling
Both services use `SemaphoreSlim` to limit concurrent operations:
```csharp
private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount);

await _semaphore.WaitAsync(ct);
try
{
    // Extract metadata/thumbnail
}
finally
{
    _semaphore.Release();
}
```

This prevents overwhelming the system when scanning large video libraries.

### Async Execution
All I/O operations run on background threads via `Task.Run()`:
```csharp
return await Task.Run(() => ExtractDuration(filePath), ct);
```

This keeps the UI thread responsive during metadata extraction.

## Error Handling Strategy

### Graceful Degradation
All methods return `null` for failures instead of throwing exceptions:
- Unsupported video formats → `null`
- Missing codecs → `null`
- Corrupted files → `null`
- Access denied → `null`

This allows the UI to display placeholder content and continue scanning other files.

### COM Object Cleanup
All COM objects are properly released in finally blocks:
```csharp
finally
{
    if (presentationDescriptor != IntPtr.Zero)
        Marshal.Release(presentationDescriptor);
    if (mediaSource != IntPtr.Zero)
        Marshal.Release(mediaSource);
    if (sourceResolver != IntPtr.Zero)
        Marshal.Release(sourceResolver);
}
```

## Supported Video Formats

Media Foundation support depends on installed codecs on the Windows system.

### Commonly Supported (Windows 10/11 Default Codecs)
- **MP4** (H.264, H.265/HEVC)
- **AVI** (various codecs)
- **WMV** (Windows Media Video)
- **MOV** (QuickTime, if codec pack installed)
- **MKV** (Matroska, limited support)

### May Require Codec Packs
- **MKV** with certain codecs
- **FLV** (Flash Video)
- **WebM**
- Various proprietary formats

### Unsupported
Formats without installed codecs will return `null` gracefully.

## Future Enhancements

### High Priority
1. **Complete Thumbnail PNG Encoding**
   - Convert raw frame buffer to RGB/RGBA
   - Use System.Drawing or WPF Imaging to create bitmap
   - Encode as PNG byte array
   - Estimated complexity: Medium

2. **Codec Information Extraction**
   - Extract video codec (H.264, HEVC, etc.)
   - Extract audio codec (AAC, MP3, etc.)
   - Extract resolution (1920x1080, etc.)
   - Add to `IVideoMetadataService` interface

3. **Thumbnail Caching**
   - In-memory LRU cache
   - Disk-based cache with invalidation
   - Significant performance improvement for large libraries

### Medium Priority
4. **Multi-Frame Thumbnail Selection**
   - Extract thumbnails from multiple timestamps
   - Choose best frame (highest contrast, not black)
   - Improves thumbnail quality

5. **Thumbnail Size Control**
   - Allow caller to specify desired thumbnail size
   - Scale during extraction for efficiency
   - Reduce memory usage

### Low Priority
6. **Hardware Acceleration**
   - Use GPU for video decoding if available
   - Significant performance improvement
   - Requires more complex MF configuration

## Testing Strategy

### Current Tests
- Interface contract tests (mocked)
- Cancellation support verification
- Null handling for unsupported formats

### Needed Tests (Future)
- Integration tests with real video files
- Performance tests with large libraries
- Codec support matrix validation
- Memory leak detection (COM object release)

## Troubleshooting

### Media Foundation Initialization Fails
**Symptom**: Exception on `MFStartup()`  
**Causes**:
- Media Foundation not available (very old Windows)
- Corrupted Windows Media Feature Pack
- Missing system DLLs

**Handling**: App displays warning and falls back to placeholder services

### No Duration Extracted
**Symptom**: All videos show "Unknown" duration  
**Causes**:
- Missing codecs for video format
- Corrupted video files
- Files are not actually video files

**Handling**: Gracefully shows "Unknown" in UI

### Memory Leaks
**Symptom**: Memory usage grows unbounded  
**Causes**:
- COM objects not released
- Media buffers not unlocked

**Prevention**: All COM objects released in finally blocks

## References

### Microsoft Documentation
- [Media Foundation Programming Guide](https://docs.microsoft.com/en-us/windows/win32/medfound/media-foundation-programming-guide)
- [IMFSourceReader](https://docs.microsoft.com/en-us/windows/win32/api/mfreadwrite/nn-mfreadwrite-imfsourcereader)
- [IMFMediaSource](https://docs.microsoft.com/en-us/windows/win32/api/mfidl/nn-mfidl-imfmediasource)

### P/Invoke Resources
- [PInvoke.net](https://www.pinvoke.net/)
- Platform Invoke (P/Invoke) documentation
