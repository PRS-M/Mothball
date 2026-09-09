using CoreApp.Domain.ValueObjects;

namespace MothballMobile.Infrastructure.Scanning;

public interface IBarcodeScanSession
{
    Task<Barcode?> ScanAsync();

    Task CompleteAsync(Barcode? barcode);

    /// <summary>
    /// Completes an active scan without navigating, for scanner-page disappearance.
    /// </summary>
    Task CancelAsync();
}
