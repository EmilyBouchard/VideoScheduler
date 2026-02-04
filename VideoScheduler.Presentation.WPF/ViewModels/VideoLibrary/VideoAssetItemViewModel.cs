using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

public partial class VideoAssetItemViewModel : ObservableObject
{
    public string FullPath { get; }
    public string FileName { get; }

    [ObservableProperty]
    private string? _durationText;

    public VideoAssetItemViewModel(string fullPath, string fileName, string? durationText)
    {
        FullPath = fullPath;
        FileName = fileName;
        _durationText = durationText;
    }
}