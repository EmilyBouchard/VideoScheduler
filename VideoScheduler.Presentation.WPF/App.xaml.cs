using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoScheduler.Application.VideoLibrary.Services;
using VideoScheduler.Infrastructure.VideoLibrary;
using VideoScheduler.Infrastructure.VideoLibrary.MediaFoundation;
using VideoScheduler.Presentation.WPF.Navigation;
using VideoScheduler.Presentation.WPF.ViewModels;
using VideoScheduler.Presentation.WPF.ViewModels.VideoLibrary;

namespace VideoScheduler.Presentation.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private MediaFoundationManager? _mediaFoundationManager;
    
    public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Host not initialized.");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        // Initialize Media Foundation first
        _mediaFoundationManager = new MediaFoundationManager();
        try
        {
            _mediaFoundationManager.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to initialize Media Foundation: {ex.Message}\n\nThe application will run with limited functionality.",
                "Media Foundation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        
        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices(services =>
            {
                // Views
                services.AddSingleton<Views.ShellView>();
                
                // ViewModels
                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<VideoLibraryViewModel>();
                
                // Navigation
                services.AddSingleton<INavigationService>(serviceProvider =>
                {
                    var shellVm = serviceProvider.GetRequiredService<ShellViewModel>();
                    return new NavigationService(
                        serviceProvider,
                        getCurrent: () => shellVm.CurrentViewModel,
                        setCurrent: vm => shellVm.CurrentViewModel = vm
                    );
                });
                
                // Application Services - Using Media Foundation implementations
                services.AddSingleton<IVideoLibraryScanner, FileSystemVideoLibraryScanner>();
                services.AddSingleton<IVideoMetadataService, MediaFoundationMetadataService>();
                services.AddSingleton<IThumbnailService, MediaFoundationThumbnailService>();
                
                // App services
                services.AddSingleton<Services.IDispatcher, Services.WpfDispatcher>();
            })
            .Build();
        
        _host.Start();
        
        var shell = Services.GetRequiredService<Views.ShellView>();
        shell.Show();

        var navigation = Services.GetRequiredService<INavigationService>();
        _ = navigation.NavigateToAsync<VideoLibraryViewModel>();
    }


    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(2));
            _host.Dispose();
        }
        
        // Shutdown Media Foundation
        _mediaFoundationManager?.Dispose();
        
        base.OnExit(e);
    }
}