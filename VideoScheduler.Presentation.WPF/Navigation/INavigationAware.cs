using System.Threading;
using System.Threading.Tasks;

namespace VideoScheduler.Presentation.WPF.Navigation;

public interface INavigationAware
{
    Task OnNavigatedToAsync(object? parameter, CancellationToken ct);
    Task OnNavigatedFromAsync(CancellationToken ct);
}
