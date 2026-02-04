using System;
using System.Runtime.InteropServices;

namespace VideoScheduler.Infrastructure.VideoLibrary.MediaFoundation;

/// <summary>
/// Manages Media Foundation lifetime for the application.
/// Must call Initialize() once at startup and Shutdown() at exit.
/// </summary>
public sealed class MediaFoundationManager : IDisposable
{
    private bool _isInitialized;
    private bool _disposed;

    // P/Invoke declarations for Media Foundation
    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int MFStartup(uint version, uint dwFlags = 0);

    [DllImport("Mfplat.dll", ExactSpelling = true)]
    private static extern int MFShutdown();

    private const uint MF_VERSION = 0x00020070; // MF_SDK_VERSION for Windows 10
    private const uint MFSTARTUP_FULL = 0;

    /// <summary>
    /// Initializes Media Foundation. Should be called once at application startup.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
            return;

        var hr = MFStartup(MF_VERSION, MFSTARTUP_FULL);
        if (hr != 0)
        {
            throw new InvalidOperationException($"Failed to initialize Media Foundation. HRESULT: 0x{hr:X8}");
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Shuts down Media Foundation. Should be called once at application exit.
    /// </summary>
    public void Shutdown()
    {
        if (!_isInitialized || _disposed)
            return;

        try
        {
            MFShutdown();
        }
        catch
        {
            // Ignore errors during shutdown
        }

        _isInitialized = false;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Shutdown();
        _disposed = true;
    }
}
