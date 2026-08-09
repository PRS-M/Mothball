using System;
using System.Threading;
using System.Threading.Tasks;

namespace MothballMobile.Infrastructure;

public sealed class Debouncer : IDebouncer, IDisposable
{
    private readonly object sync = new();
    private CancellationTokenSource? cts;
    private readonly int delayMs;
    private bool isDisposed;

    public Debouncer(int delayMs)
    {
        this.delayMs = delayMs;
    }

    public Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (isDisposed)
        {
            return Task.CompletedTask;
        }

        CancellationTokenSource localCts;
        lock (sync)
        {
            if (isDisposed)
            {
                return Task.CompletedTask;
            }

            cts?.Cancel();
            cts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            localCts = cts;
        }

        return DebounceCoreAsync(action, localCts);
    }

    private async Task DebounceCoreAsync(Func<CancellationToken, Task> action, CancellationTokenSource localCts)
    {
        var token = localCts.Token;

        try
        {
            await Task.Delay(delayMs, token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await action(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Ignore cancellation from newer debounced requests or disposal.
        }
        finally
        {
            lock (sync)
            {
                if (ReferenceEquals(cts, localCts))
                {
                    cts.Dispose();
                    cts = null;
                }
            }
        }
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        CancellationTokenSource? toDispose;
        lock (sync)
        {
            toDispose = cts;
            cts = null;
        }

        try
        {
            toDispose?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // already disposed
        }
        finally
        {
            toDispose?.Dispose();
        }
    }
}
