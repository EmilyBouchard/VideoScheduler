using System;
using System.Threading.Tasks;

namespace VideoScheduler.Presentation.WPF.Services;

public interface IDispatcher
{
    void Invoke(Action action);
    Task InvokeAsync(Action action);
}
