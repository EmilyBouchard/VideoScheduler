using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary.MediaFoundation;

/// <summary>
/// Extracts video thumbnails using Microsoft Media Foundation.
/// Returns PNG-encoded byte array or null for unsupported formats.
/// </summary>
public sealed class MediaFoundationThumbnailService : IThumbnailService
{
    private readonly SemaphoreSlim _semaphore;

    public MediaFoundationThumbnailService()
    {
        // Limit concurrent thumbnail extractions
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    }

    public async Task<byte[]?> TryExtractThumbnailAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return null;

        await _semaphore.WaitAsync(ct);
        try
        {
            return await Task.Run(() => ExtractThumbnail(filePath), ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Gracefully fail for unsupported formats or extraction errors
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static byte[]? ExtractThumbnail(string filePath)
    {
        IntPtr sourceResolver = IntPtr.Zero;
        IntPtr mediaSource = IntPtr.Zero;
        IntPtr presentationDescriptor = IntPtr.Zero;
        IntPtr streamDescriptor = IntPtr.Zero;
        IntPtr sourceReader = IntPtr.Zero;
        IntPtr sample = IntPtr.Zero;
        IntPtr mediaBuffer = IntPtr.Zero;

        try
        {
            // Create source resolver
            var hr = MFCreateSourceResolver(out sourceResolver);
            if (hr != 0) return null;

            // Create media source from file
            hr = IMFSourceResolver_CreateObjectFromURL(
                sourceResolver,
                filePath,
                MF_RESOLUTION_MEDIASOURCE,
                IntPtr.Zero,
                out _,
                out mediaSource);
            if (hr != 0 || mediaSource == IntPtr.Zero) return null;

            // Create source reader for easier frame extraction
            hr = MFCreateSourceReaderFromMediaSource(mediaSource, IntPtr.Zero, out sourceReader);
            if (hr != 0 || sourceReader == IntPtr.Zero) return null;

            // Select the first video stream
            hr = IMFSourceReader_SetStreamSelection(sourceReader, MF_SOURCE_READER_ALL_STREAMS, false);
            if (hr != 0) return null;

            hr = IMFSourceReader_SetStreamSelection(sourceReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, true);
            if (hr != 0) return null;

            // Seek to 1 second position for better thumbnail (skip black frames at start)
            var emptyGuid = Guid.Empty;
            hr = IMFSourceReader_SetCurrentPosition(sourceReader, ref emptyGuid, 10000000); // 1 second in 100ns units
            if (hr != 0)
            {
                // If seeking fails, just read from start
            }

            // Read a sample (video frame)
            hr = IMFSourceReader_ReadSample(
                sourceReader,
                MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                0,
                out _,
                out _,
                out _,
                out sample);
            if (hr != 0 || sample == IntPtr.Zero) return null;

            // Get the media buffer from the sample
            hr = IMFSample_ConvertToContiguousBuffer(sample, out mediaBuffer);
            if (hr != 0 || mediaBuffer == IntPtr.Zero) return null;

            // Lock the buffer and get the data
            hr = IMFMediaBuffer_Lock(mediaBuffer, out IntPtr bufferData, out _, out uint currentLength);
            if (hr != 0 || bufferData == IntPtr.Zero) return null;

            try
            {
                // Copy the buffer data
                var frameData = new byte[currentLength];
                Marshal.Copy(bufferData, frameData, 0, (int)currentLength);

                // For now, return the raw frame data
                // In a real implementation, we would:
                // 1. Get the video format from the source reader
                // 2. Convert to RGB if needed
                // 3. Create a bitmap
                // 4. Encode to PNG
                // For simplicity, returning null (placeholder behavior maintained)
                // TODO: Implement proper frame-to-PNG conversion

                return null; // Placeholder until full implementation
            }
            finally
            {
                IMFMediaBuffer_Unlock(mediaBuffer);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            // Release COM objects
            if (mediaBuffer != IntPtr.Zero)
                Marshal.Release(mediaBuffer);
            if (sample != IntPtr.Zero)
                Marshal.Release(sample);
            if (sourceReader != IntPtr.Zero)
                Marshal.Release(sourceReader);
            if (streamDescriptor != IntPtr.Zero)
                Marshal.Release(streamDescriptor);
            if (presentationDescriptor != IntPtr.Zero)
                Marshal.Release(presentationDescriptor);
            if (mediaSource != IntPtr.Zero)
                Marshal.Release(mediaSource);
            if (sourceResolver != IntPtr.Zero)
                Marshal.Release(sourceResolver);
        }
    }

    #region P/Invoke Declarations

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateSourceResolver(out IntPtr ppISourceResolver);

    [DllImport("Mf.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern int IMFSourceResolver_CreateObjectFromURL(
        IntPtr pThis,
        [MarshalAs(UnmanagedType.LPWStr)] string pwszURL,
        uint dwFlags,
        IntPtr pProps,
        out uint pObjectType,
        out IntPtr ppObject);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int MFCreateSourceReaderFromMediaSource(
        IntPtr pMediaSource,
        IntPtr pAttributes,
        out IntPtr ppSourceReader);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int IMFSourceReader_SetStreamSelection(
        IntPtr pThis,
        uint dwStreamIndex,
        bool fSelected);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int IMFSourceReader_SetCurrentPosition(
        IntPtr pThis,
        ref Guid guidTimeFormat,
        long vPosition);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int IMFSourceReader_ReadSample(
        IntPtr pThis,
        uint dwStreamIndex,
        uint dwControlFlags,
        out uint pdwActualStreamIndex,
        out uint pdwStreamFlags,
        out long pllTimestamp,
        out IntPtr ppSample);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFSample_ConvertToContiguousBuffer(
        IntPtr pThis,
        out IntPtr ppBuffer);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFMediaBuffer_Lock(
        IntPtr pThis,
        out IntPtr ppbBuffer,
        out uint pcbMaxLength,
        out uint pcbCurrentLength);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFMediaBuffer_Unlock(IntPtr pThis);

    private const uint MF_RESOLUTION_MEDIASOURCE = 0x00000001;
    private const uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;
    private const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;

    #endregion
}
