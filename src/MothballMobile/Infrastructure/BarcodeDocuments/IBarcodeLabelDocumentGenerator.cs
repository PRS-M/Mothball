namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Generates printable PDF documents containing inventory barcode labels.
/// </summary>
public interface IBarcodeLabelDocumentGenerator
{
    /// <summary>
    /// Generates a PDF for the supplied labels.
    /// </summary>
    /// <param name="labels">The labels to render in document order.</param>
    /// <param name="fileName">The output file name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<BarcodeDocumentResult> GenerateAsync(
        IReadOnlyCollection<BarcodeLabelData> labels,
        string fileName,
        CancellationToken cancellationToken = default);
}
