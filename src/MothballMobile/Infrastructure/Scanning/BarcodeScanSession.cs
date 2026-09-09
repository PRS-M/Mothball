using CoreApp.Domain.ValueObjects;

namespace MothballMobile.Infrastructure.Scanning;

public sealed class BarcodeScanSession : IBarcodeScanSession
{
    private readonly INavigationService navigation;
    private readonly SemaphoreSlim sessionGate = new(1, 1);
    private TaskCompletionSource<Barcode?>? pendingResult;
    private int completionRequested;

    public BarcodeScanSession(INavigationService navigation)
    {
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public async Task<Barcode?> ScanAsync()
    {
        await sessionGate.WaitAsync().ConfigureAwait(false);
        try
        {
            pendingResult = new TaskCompletionSource<Barcode?>(TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref completionRequested, 0);
            await navigation.GoToAsync(NavigationRoutes.BarcodeScanner).ConfigureAwait(false);

            return await pendingResult.Task.ConfigureAwait(false);
        }
        finally
        {
            pendingResult = null;
            Volatile.Write(ref completionRequested, 0);
            sessionGate.Release();
        }
    }

    public async Task CompleteAsync(Barcode? barcode)
    {
        var result = pendingResult ?? throw new InvalidOperationException("There is no active barcode scan.");
        Interlocked.Exchange(ref completionRequested, 1);
        try
        {
            await navigation.GoBackAsync().ConfigureAwait(false);
            result.TrySetResult(barcode);
        }
        catch
        {
            result.TrySetResult(null);
            throw;
        }
    }

    public Task CancelAsync()
    {
        if (Volatile.Read(ref completionRequested) == 0)
        {
            pendingResult?.TrySetResult(null);
        }

        return Task.CompletedTask;
    }
}
