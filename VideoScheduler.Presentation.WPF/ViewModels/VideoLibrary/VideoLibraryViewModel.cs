using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

public partial class VideoLibraryViewModel : ObservableObject
{
    private readonly IVideoLibraryScanner _scanner;
    private readonly IVideoMetadataService _metadataService;
    private readonly IThumbnailService _thumbnailService;

    private CancellationTokenSource? _loadCts;
    
    public VideoLibraryViewModel(
        IVideoLibraryScanner scanner, 
        IVideoMetadataService metadataService,
        IThumbnailService thumbnailService)
    {
        _scanner = scanner;
        _metadataService = metadataService;
        _thumbnailService = thumbnailService;
    }

    public ObservableCollection<FolderNodeViewModel> Folders { get; } = new();
    public ObservableCollection<VideoAssetItemViewModel> Videos { get; } = new();

    [ObservableProperty]
    private string? _rootFolder;
    
    [ObservableProperty]
    private FolderNodeViewModel? _selectedFolder;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusText;

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "Select Folder",
            ValidateNames = false,
            Title = "Select Video Library Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            var folderPath = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(folderPath))
            {
                RootFolder = folderPath;
            }
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(RootFolder))
            return;
        
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;
        
        IsLoading = true;
        StatusText = "Scanning...";
        Folders.Clear();
        Videos.Clear();

        try
        {
            await foreach (var folder in _scanner.EnumerateFoldersAsync(RootFolder, ct))
            {
                Folders.Add(new FolderNodeViewModel(folder.FullPath, folder.Name));
            }

            await foreach (var video in _scanner.EnumerateVideosAsync(RootFolder, ct))
            {
                var duration = await _metadataService.TryGetVideoDurationAsync(video.FullPath, ct);
                var durationText = duration is null ? "Unknown" : duration.Value.ToString(@"hh\:mm\:ss");

                var videoVm = new VideoAssetItemViewModel(
                    video.FullPath, 
                    video.FileName,
                    video.SizeInBytes,
                    video.LastWriteTimeUtc,
                    durationText);

                Videos.Add(videoVm);

                // Load thumbnail asynchronously in the background
                _ = LoadThumbnailAsync(videoVm, ct);
            }

            StatusText = $"Found {Videos.Count} videos.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Scan canceled.";
        }
        catch (Exception e)
        {
            StatusText = $"Error: {e.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadThumbnailAsync(VideoAssetItemViewModel videoVm, CancellationToken ct)
    {
        try
        {
            videoVm.IsThumbnailLoading = true;
            
            var thumbnailBytes = await _thumbnailService.TryExtractThumbnailAsync(videoVm.FullPath, ct);
            
            if (thumbnailBytes != null && thumbnailBytes.Length > 0)
            {
                // Must create BitmapImage on a background thread and freeze it
                await Task.Run(() =>
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new MemoryStream(thumbnailBytes);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze(); // Required for cross-thread access
                    
                    videoVm.ThumbnailImage = bitmap;
                }, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch
        {
            // Silently fail - thumbnail will remain null
        }
        finally
        {
            videoVm.IsThumbnailLoading = false;
        }
    }

    partial void OnSelectedFolderChanged(FolderNodeViewModel? value)
    {
        if (value is null)
            return;

        RootFolder = value.FullPath;
        _ = LoadCommand.ExecuteAsync(null);
    }
}
