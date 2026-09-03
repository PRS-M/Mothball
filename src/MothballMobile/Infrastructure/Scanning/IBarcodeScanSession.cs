using CoreApp.Domain.ValueObjects;

namespace MothballMobile.Infrastructure.Scanning;

public interface IBarcodeScanSession
{
    Task<Barcode?> ScanAsync();

    Task CompleteAsync(Barcode? barcode);
}