using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoScheduler.Presentation.WPF.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private string title = "Video Scheduler";
    
    [ObservableProperty]
    private object? currentViewModel;
}
