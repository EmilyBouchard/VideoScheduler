using System.Threading.Tasks;

namespace VideoScheduler.Presentation.WPF.Navigation;

public interface INavigationService
{
    object? CurrentViewModel { get; }
    
    Task NavigateToAsync<TViewModel>() where TViewModel : class;
    Task NavigateToAsync<TViewModel>(object? parameter) where TViewModel : class;
}
