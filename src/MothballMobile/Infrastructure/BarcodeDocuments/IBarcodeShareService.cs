using CoreApp.Domain.Entities.Shared;

namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Generates and shares barcode label documents through the device share sheet.
/// </summary>
public interface IBarcodeShareService
{
    /// <summary>
    /// Generates and shares one barcode label.
    /// </summary>
    /// <param name="name">The inventory record name.</param>
    /// <param name="barcode">The barcode to render.</param>
    Task ShareAsync(string name, Barcode barcode);
}
