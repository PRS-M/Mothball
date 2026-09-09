namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Serializes barcode registry and inventory-entity updates within one application process.
/// </summary>
public sealed class BarcodeOperationCoordinator
{
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>
    /// Acquires the barcode-operation gate until the returned lease is disposed.
    /// </summary>
    public async Task<IDisposable> AcquireAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        return new Lease(gate);
    }

    private sealed class Lease : IDisposable
    {
        private readonly SemaphoreSlim gate;
        private int disposed;

        public Lease(SemaphoreSlim gate)
        {
            this.gate = gate;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                gate.Release();
            }
        }
    }
}
