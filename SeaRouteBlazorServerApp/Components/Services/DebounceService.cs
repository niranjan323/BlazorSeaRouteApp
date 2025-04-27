namespace SeaRouteBlazorServerApp.Components.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

public class DebounceService : IDisposable
{
    private Timer _timer;
    private int _debounceDelay;

    public DebounceService(int debounceDelay = 300)
    {
        _debounceDelay = debounceDelay;
    }

    public void Debounce(Func<Task> action)
    {
        _timer?.Dispose();
        _timer = new Timer(async _ =>
        {
            _timer?.Dispose();
            await action.Invoke();
        }, null, _debounceDelay, Timeout.Infinite);
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

