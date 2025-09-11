using System;
using System.Threading;
using System.Threading.Tasks;

namespace MothballMobile.Infrastructure;

public sealed class Debouncer : IDebouncer, IDisposable
{
    private CancellationTokenSource? cts;
    private readonly int delayMs;
    private bool isDisposed;

    public Debouncer(int delayMs)
    {
        this.delayMs = delayMs;
    }

    public void Debounce(Action action)
    {
        if (isDisposed)
        {
            return;
        }

        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // If previously disposed, ignore and proceed with a fresh CTS
        }

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
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        try
        {
            cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // already disposed
        }
        finally
        {
            cts?.Dispose();
            cts = null;
        }
    }
}
