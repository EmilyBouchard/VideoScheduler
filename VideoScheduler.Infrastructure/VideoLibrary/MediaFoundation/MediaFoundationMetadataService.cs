using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary.MediaFoundation;

/// <summary>
/// Extracts video metadata using Microsoft Media Foundation.
/// Gracefully handles unsupported formats by returning null.
/// </summary>
public sealed class MediaFoundationMetadataService : IVideoMetadataService
{
    private readonly SemaphoreSlim _semaphore;

    public MediaFoundationMetadataService()
    {
        // Limit concurrent MF operations to avoid overwhelming the system
        _semaphore = new SemaphoreSlim(Environment.ProcessorCount);
    }

    public async Task<TimeSpan?> TryGetVideoDurationAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return null;

        await _semaphore.WaitAsync(ct);
        try
        {
            return await Task.Run(() => ExtractDuration(filePath), ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // Gracefully fail for unsupported formats
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static TimeSpan? ExtractDuration(string filePath)
    {
        IntPtr sourceResolver = IntPtr.Zero;
        IntPtr mediaSource = IntPtr.Zero;
        IntPtr presentationDescriptor = IntPtr.Zero;

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

            // Get presentation descriptor
            hr = IMFMediaSource_CreatePresentationDescriptor(mediaSource, out presentationDescriptor);
            if (hr != 0 || presentationDescriptor == IntPtr.Zero) return null;

            // Get duration attribute
            var durationGuid = new Guid(0x6c990d33, 0xbb8e, 0x477a,
                0x85, 0x98, 0xd, 0x5d, 0x96, 0xfc, 0xd8, 0x8a);
            hr = IMFPresentationDescriptor_GetUINT64(
                presentationDescriptor,
                ref durationGuid,
                out long duration100ns);
            if (hr != 0) return null;

            // Convert 100-nanosecond units to TimeSpan
            return TimeSpan.FromTicks(duration100ns / 10);
        }
        catch
        {
            return null;
        }
        finally
        {
            // Release COM objects
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

    [DllImport("Mf.dll", ExactSpelling = true)]
    private static extern int IMFMediaSource_CreatePresentationDescriptor(
        IntPtr pThis,
        out IntPtr ppPresentationDescriptor);

    [DllImport("Mf.dll", ExactSpelling = true)]
    private static extern int IMFPresentationDescriptor_GetUINT64(
        IntPtr pThis,
        ref Guid guidKey,
        out long punValue);

    private const uint MF_RESOLUTION_MEDIASOURCE = 0x00000001;
    private const uint MF_SOURCE_READER_ALL_STREAMS = 0xFFFFFFFE;
    private const uint MF_SOURCE_READER_FIRST_VIDEO_STREAM = 0xFFFFFFFC;

    #endregion
}
