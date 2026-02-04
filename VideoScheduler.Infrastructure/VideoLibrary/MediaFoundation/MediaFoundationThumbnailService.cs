using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Infrastructure.VideoLibrary.MediaFoundation;

/// <summary>
/// Extracts video thumbnails using Microsoft Media Foundation.
/// Returns PNG-encoded byte array or null for unsupported formats.
/// </summary>
public sealed class MediaFoundationThumbnailService : IThumbnailService
{
    private readonly SemaphoreSlim _semaphore;
    private const int ThumbnailWidth = 320;
    private const int ThumbnailHeight = 180;

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
        IntPtr sourceReader = IntPtr.Zero;
        IntPtr sample = IntPtr.Zero;
        IntPtr mediaBuffer = IntPtr.Zero;
        IntPtr mediaType = IntPtr.Zero;
        IntPtr attributes = IntPtr.Zero;

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

            // Create attributes for source reader configuration
            hr = MFCreateAttributes(out attributes, 1);
            if (hr != 0) return null;

            // Enable video processing to allow format conversion
            var enableVideoProcessingGuid = new Guid(0xfb394f3d, 0xccf1, 0x42ee, 0xbb, 0xb3, 0xf9, 0xb8, 0x45, 0xd5, 0x68, 0x1d);
            hr = IMFAttributes_SetUINT32(attributes, ref enableVideoProcessingGuid, 1);
            if (hr != 0) return null;

            // Create source reader with video processing enabled
            hr = MFCreateSourceReaderFromMediaSource(mediaSource, attributes, out sourceReader);
            if (hr != 0 || sourceReader == IntPtr.Zero) return null;

            // Select the first video stream
            hr = IMFSourceReader_SetStreamSelection(sourceReader, MF_SOURCE_READER_ALL_STREAMS, false);
            if (hr != 0) return null;

            hr = IMFSourceReader_SetStreamSelection(sourceReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, true);
            if (hr != 0) return null;

            // Configure output format to RGB32
            hr = MFCreateMediaType(out mediaType);
            if (hr != 0) return null;

            var majorTypeGuid = new Guid(0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71); // MFMediaType_Video
            var mtMajorType = new Guid(0x48eba18e, 0xf8c9, 0x4687, 0xbf, 0x11, 0x0a, 0x74, 0xc9, 0xf9, 0x6a, 0x8f);
            hr = IMFMediaType_SetGUID(mediaType, ref mtMajorType, ref majorTypeGuid);
            if (hr != 0) return null;

            var rgb32Guid = new Guid(0x00000016, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71); // MFVideoFormat_RGB32
            var mtSubtype = new Guid(0xf7e34c9a, 0x42e8, 0x4714, 0xb7, 0x4b, 0xcb, 0x29, 0xd7, 0x2c, 0x35, 0xe5);
            hr = IMFMediaType_SetGUID(mediaType, ref mtSubtype, ref rgb32Guid);
            if (hr != 0) return null;

            // Set the media type on the source reader
            hr = IMFSourceReader_SetCurrentMediaType(sourceReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, IntPtr.Zero, mediaType);
            if (hr != 0) return null;

            // Get the actual media type (to read width and height)
            Marshal.Release(mediaType);
            mediaType = IntPtr.Zero;
            hr = IMFSourceReader_GetCurrentMediaType(sourceReader, MF_SOURCE_READER_FIRST_VIDEO_STREAM, out mediaType);
            if (hr != 0 || mediaType == IntPtr.Zero) return null;

            // Get frame dimensions
            ulong frameSize = 0;
            var mtFrameSize = new Guid(0x1652c33d, 0xd6b2, 0x4012, 0xb8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xa3, 0x7d);
            hr = IMFMediaType_GetUINT64(mediaType, ref mtFrameSize, out frameSize);
            if (hr != 0) return null;

            uint width = (uint)(frameSize >> 32);
            uint height = (uint)(frameSize & 0xFFFFFFFF);

            if (width == 0 || height == 0 || width > 8192 || height > 8192)
                return null;

            // Get stride (bytes per row)
            int stride;
            var mtDefaultStride = new Guid(0x644b4e48, 0x1e02, 0x4516, 0xb0, 0xeb, 0xc0, 0x1c, 0xa9, 0xd4, 0x9a, 0xc6);
            hr = IMFMediaType_GetUINT32(mediaType, ref mtDefaultStride, out uint strideUint);
            if (hr == 0)
            {
                stride = (int)strideUint;
            }
            else
            {
                // Calculate stride for RGB32 (4 bytes per pixel)
                stride = (int)width * 4;
            }

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
                // Calculate expected buffer size
                int expectedSize = Math.Abs(stride) * (int)height;
                if (currentLength < expectedSize)
                    return null;

                // Copy the frame data
                var frameData = new byte[expectedSize];
                Marshal.Copy(bufferData, frameData, 0, expectedSize);

                // Convert to PNG
                return ConvertRgb32ToPng(frameData, (int)width, (int)height, stride);
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
            if (attributes != IntPtr.Zero)
                Marshal.Release(attributes);
            if (mediaType != IntPtr.Zero)
                Marshal.Release(mediaType);
            if (mediaBuffer != IntPtr.Zero)
                Marshal.Release(mediaBuffer);
            if (sample != IntPtr.Zero)
                Marshal.Release(sample);
            if (sourceReader != IntPtr.Zero)
                Marshal.Release(sourceReader);
            if (mediaSource != IntPtr.Zero)
                Marshal.Release(mediaSource);
            if (sourceResolver != IntPtr.Zero)
                Marshal.Release(sourceResolver);
        }
    }

    private static byte[]? ConvertRgb32ToPng(byte[] rgbData, int width, int height, int stride)
    {
        try
        {
            // RGB32 in Media Foundation is BGRA (4 bytes per pixel, bottom-up)
            // WPF expects top-down, so we need to flip if stride is negative
            bool isBottomUp = stride < 0;
            int absStride = Math.Abs(stride);

            // Create bitmap source from RGB data
            BitmapSource bitmap;
            
            if (isBottomUp)
            {
                // Flip the image vertically
                var flippedData = new byte[absStride * height];
                for (int y = 0; y < height; y++)
                {
                    int srcOffset = y * absStride;
                    int dstOffset = (height - 1 - y) * absStride;
                    Array.Copy(rgbData, srcOffset, flippedData, dstOffset, absStride);
                }
                
                bitmap = BitmapSource.Create(
                    width, height,
                    96, 96, // DPI
                    PixelFormats.Bgr32,
                    null,
                    flippedData,
                    absStride);
            }
            else
            {
                bitmap = BitmapSource.Create(
                    width, height,
                    96, 96, // DPI
                    PixelFormats.Bgr32,
                    null,
                    rgbData,
                    absStride);
            }

            // Scale down if needed (for performance)
            if (width > ThumbnailWidth || height > ThumbnailHeight)
            {
                double scale = Math.Min((double)ThumbnailWidth / width, (double)ThumbnailHeight / height);
                int newWidth = (int)(width * scale);
                int newHeight = (int)(height * scale);
                
                var scaledBitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                bitmap = scaledBitmap;
            }

            // Freeze for thread safety
            bitmap.Freeze();

            // Encode to PNG
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            return stream.ToArray();
        }
        catch
        {
            return null;
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

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateAttributes(out IntPtr ppMFAttributes, uint cInitialSize);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFAttributes_SetUINT32(IntPtr pThis, ref Guid guidKey, uint unValue);

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

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int MFCreateMediaType(out IntPtr ppMFType);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFMediaType_SetGUID(IntPtr pThis, ref Guid guidKey, ref Guid guidValue);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int IMFSourceReader_SetCurrentMediaType(
        IntPtr pThis,
        uint dwStreamIndex,
        IntPtr pdwReserved,
        IntPtr pMediaType);

    [DllImport("Mfreadwrite.dll", ExactSpelling = true)]
    private static extern int IMFSourceReader_GetCurrentMediaType(
        IntPtr pThis,
        uint dwStreamIndex,
        out IntPtr ppMediaType);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFMediaType_GetUINT64(IntPtr pThis, ref Guid guidKey, out ulong punValue);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int IMFMediaType_GetUINT32(IntPtr pThis, ref Guid guidKey, out uint punValue);

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
