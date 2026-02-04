---
applyTo: "VideoScheduler.Presentation.WPF/**/*.cs,VideoScheduler.Presentation.WPF/**/*.xaml"
---

# Presentation (WPF) Layer Instructions

## Purpose
The Presentation layer provides the **WPF user interface** using strict MVVM patterns with CommunityToolkit.Mvvm.

## MVVM Rules (Critical)
- ViewModels **inherit from ObservableObject**
- Use **[ObservableProperty]** for bindable properties (not manual INotifyPropertyChanged)
- Use **[RelayCommand]** for command binding (not manual ICommand)
- Views contain **NO business logic** - only InitializeComponent and DI wiring
- ViewModels **never reference View types** (no UserControl, Window, etc.)
- Navigation is **ViewModel-first** via INavigationService

## Navigation
- Use `INavigationService.NavigateAsync<TViewModel>()` to change views
- ViewModels implement `INavigationAware` for lifecycle hooks:
  - `OnNavigatedToAsync` - called when navigated to
  - `OnNavigatedFromAsync` - called before navigating away (can cancel)
- Views are resolved via **DataTemplates** in App.xaml (keyed by ViewModel type)
- Never hard-code View instantiation in navigation code

## UI Performance
- Use **VirtualizingStackPanel** for lists
- **Freeze** BitmapImage objects created off-thread before assigning to UI
- Use **async/await** for I/O - never block UI thread
- Throttle operations with `SemaphoreSlim` or debouncing
- Use `ConfigureAwait(false)` for non-UI work, then marshal back to UI thread

## Patterns
```csharp
// ViewModel
public partial class VideoLibraryViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty]
    private string _searchText = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<VideoAsset> _videos = [];
    
    [RelayCommand]
    private async Task LoadVideosAsync(CancellationToken cancellationToken)
    {
        // Use service to load data
        await foreach (var video in _videoService.ScanFolderAsync(folderPath, cancellationToken))
        {
            Videos.Add(video);
        }
    }
    
    public async Task OnNavigatedToAsync(CancellationToken cancellationToken)
    {
        await LoadVideosCommand.ExecuteAsync(cancellationToken);
    }
    
    public Task<bool> OnNavigatedFromAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(true); // true = allow navigation
    }
}

// View code-behind (minimal)
public partial class VideoLibraryView : UserControl
{
    public VideoLibraryView()
    {
        InitializeComponent();
    }
}
```

## ResourceDictionaries
- Split styles into separate files:
  - Colors.xaml - color definitions
  - Typography.xaml - text styles
  - Controls.xaml - control templates
  - Templates.xaml - DataTemplates for VM→View mapping
- Merge in App.xaml: `<ResourceDictionary.MergedDictionaries>`

## What NOT to do
- ❌ Don't put business logic in code-behind
- ❌ Don't manually implement INotifyPropertyChanged - use [ObservableProperty]
- ❌ Don't create RelayCommand manually - use [RelayCommand]
- ❌ Don't reference View types in ViewModels
- ❌ Don't use Dispatcher.Invoke - use async binding or Task-based commands
- ❌ Don't hard-code navigation - use INavigationService
- ❌ Don't forget to Freeze() bitmap images created off-thread
