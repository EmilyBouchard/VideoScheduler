using System;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

public partial class VideoAssetItemViewModel : ObservableObject
{
    public string FullPath { get; }
    public string FileName { get; }
    public long SizeInBytes { get; }
    public DateTimeOffset LastModified { get; }

    [ObservableProperty]
    private string? _durationText;

    [ObservableProperty]
    private BitmapImage? _thumbnailImage;

    [ObservableProperty]
    private bool _isThumbnailLoading;

    [ObservableProperty]
    private string? _fileSizeText;

    [ObservableProperty]
    private string? _lastModifiedText;

    public VideoAssetItemViewModel(
        string fullPath, 
        string fileName, 
        long sizeInBytes,
        DateTimeOffset lastModified,
        string? durationText)
    {
        FullPath = fullPath;
        FileName = fileName;
        SizeInBytes = sizeInBytes;
        LastModified = lastModified;
        _durationText = durationText;
        
        // Format display strings
        _fileSizeText = FormatFileSize(sizeInBytes);
        _lastModifiedText = lastModified.LocalDateTime.ToString("g"); // Short date and time
    }

    private static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{bytes / (double)GB:F2} GB";
        if (bytes >= MB)
            return $"{bytes / (double)MB:F2} MB";
        if (bytes >= KB)
            return $"{bytes / (double)KB:F2} KB";
        
        return $"{bytes} bytes";
    }
}