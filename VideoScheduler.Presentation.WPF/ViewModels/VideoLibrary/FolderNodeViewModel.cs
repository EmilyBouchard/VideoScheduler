using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

public partial class FolderNodeViewModel : ObservableObject
{
    public string FullPath { get; }
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;
    
    public FolderNodeViewModel(string fullPath, string name)
    {
        FullPath = fullPath;
        Name = name;
    }
}