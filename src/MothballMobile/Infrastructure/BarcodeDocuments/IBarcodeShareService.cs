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

    /// <summary>
    /// Generates and shares a batch barcode label document.
    /// </summary>
    /// <param name="labels">The labels to include in the document.</param>
    /// <param name="title">The title shown by the device share sheet.</param>
    Task ShareAsync(IReadOnlyCollection<BarcodeLabelData> labels, string title);
}
