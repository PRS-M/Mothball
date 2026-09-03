using CoreApp.Domain.ValueObjects;
using MothballMobile.Infrastructure.Navigation;

namespace MothballMobile.Infrastructure.Scanning;

public sealed class BarcodeScanSession : IBarcodeScanSession
{
    private readonly INavigationService navigation;
    private readonly SemaphoreSlim sessionGate = new(1, 1);
    private TaskCompletionSource<Barcode?>? pendingResult;

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
            await navigation.GoToAsync(NavigationRoutes.BarcodeScanner).ConfigureAwait(false);
            return await pendingResult.Task.ConfigureAwait(false);
        }
        finally
        {
            pendingResult = null;
            sessionGate.Release();
        }
    }

    public async Task CompleteAsync(Barcode? barcode)
    {
        var result = pendingResult ?? throw new InvalidOperationException("There is no active barcode scan.");
        await navigation.GoBackAsync().ConfigureAwait(false);
        result.TrySetResult(barcode);
    }
}