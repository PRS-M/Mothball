using CoreApp.Domain.Entities.Shared;

namespace MothballMobile.Infrastructure.Scanning;

public interface IBarcodeScanSession
{
    Task<Barcode?> ScanAsync();

    Task CompleteAsync(Barcode? barcode);
}