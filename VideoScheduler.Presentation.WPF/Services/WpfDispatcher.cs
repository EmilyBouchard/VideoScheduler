using System;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace VideoScheduler.Presentation.WPF.Services;

public sealed class WpfDispatcher : IDispatcher
{
    private Dispatcher Dispatcher => System.Windows.Application.Current.Dispatcher;

    public void Invoke(Action action)
    {
        if (Dispatcher.CheckAccess()) action();
        else Dispatcher.Invoke(action);
    }

    public Task InvokeAsync(Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        
        return Dispatcher.InvokeAsync(action).Task;
    }
}
