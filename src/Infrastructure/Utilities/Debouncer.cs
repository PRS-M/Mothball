using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Utilities;

public sealed class Debouncer : IDebouncer, IDisposable
{
    private CancellationTokenSource? cts;
    private readonly int delayMs;

    public Debouncer(int delayMs)
    {
        this.delayMs = delayMs;
    }

    public void Debounce(Action action)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;
                action();
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
        }, token);
    }

    public void Dispose()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
