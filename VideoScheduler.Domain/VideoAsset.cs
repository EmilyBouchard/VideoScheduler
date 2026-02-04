using System;

namespace VideoScheduler.Domain;

/// <summary>
/// Represents a video file in the domain model.
/// </summary>
public sealed class VideoAsset
{
    public VideoAsset(string filePath, string fileName, long sizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
        
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be null or whitespace.", nameof(fileName));
        
        if (sizeInBytes < 0)
            throw new ArgumentException("Size must be non-negative.", nameof(sizeInBytes));

        FilePath = filePath;
        FileName = fileName;
        SizeInBytes = sizeInBytes;
    }

    public string FilePath { get; }
    public string FileName { get; }
    public long SizeInBytes { get; }

    public string GetSizeDisplayString()
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (SizeInBytes >= GB)
            return $"{SizeInBytes / (double)GB:F2} GB";
        if (SizeInBytes >= MB)
            return $"{SizeInBytes / (double)MB:F2} MB";
        if (SizeInBytes >= KB)
            return $"{SizeInBytes / (double)KB:F2} KB";
        
        return $"{SizeInBytes} bytes";
    }
}
