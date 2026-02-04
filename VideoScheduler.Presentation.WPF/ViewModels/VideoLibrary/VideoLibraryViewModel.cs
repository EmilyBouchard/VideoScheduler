using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoScheduler.Application.VideoLibrary.Services;

namespace VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

public partial class VideoLibraryViewModel : ObservableObject
{
    private readonly IVideoLibraryScanner _scanner;
    private readonly IVideoMetadataService _metadataService;

    private CancellationTokenSource? _loadCts;
    
    public VideoLibraryViewModel(IVideoLibraryScanner scanner, IVideoMetadataService metadataService)
    {
        _scanner = scanner;
        _metadataService = metadataService;
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
                var durationText = duration is null ? null : duration.Value.ToString(@"hh\:mm\:ss");

                Videos.Add(new VideoAssetItemViewModel(video.FullPath, video.FileName, durationText));
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

    partial void OnSelectedFolderChanged(FolderNodeViewModel? value)
    {
        if (value is null)
            return;

        RootFolder = value.FullPath;
        _ = LoadCommand.ExecuteAsync(null);
    }
}
