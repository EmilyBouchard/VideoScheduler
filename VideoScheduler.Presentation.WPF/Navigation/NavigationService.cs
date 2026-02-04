using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace VideoScheduler.Presentation.WPF.Navigation;

public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly Func<object?> _getCurrent;
    private readonly Action<object?> _setCurrent;

    private CancellationTokenSource? _navigationCts;

    public NavigationService(
        IServiceProvider services,
        Func<object?> getCurrent,
        Action<object?> setCurrent)
    {
        _services = services;
        _getCurrent = getCurrent;
        _setCurrent = setCurrent;
    }
    
    public object? CurrentViewModel => _getCurrent();

    public Task NavigateToAsync<TViewModel>() where TViewModel : class
        => NavigateToAsync<TViewModel>(parameter: null);

    public async Task NavigateToAsync<TViewModel>(object? parameter) where TViewModel : class
    {
        // cancel any ongoing navigation intitialization
        _navigationCts?.Cancel();
        _navigationCts?.Dispose();
        _navigationCts = new CancellationTokenSource();
        var cancellationToken = _navigationCts.Token;
        
        var oldViewModel = _getCurrent();
        if (oldViewModel is INavigationAware oldAware)
        {
            try
            {
                await oldAware.OnNavigatedFromAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // THIS IS FINE 🔥
            }
        }
        
        // Resolve VM via DI
        var newViewModel = _services.GetRequiredService<TViewModel>();
        _setCurrent(newViewModel);
        
        if (newViewModel is INavigationAware newAware)
        {
            try
            {
                await newAware.OnNavigatedToAsync(parameter, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // THIS IS FINE 🔥
            }
        }
    }
}