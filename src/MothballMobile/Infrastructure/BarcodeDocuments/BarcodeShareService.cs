using CoreApp.Domain.Entities.Shared;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Shares generated barcode label PDFs using the native device share sheet.
/// </summary>
public sealed class BarcodeShareService : IBarcodeShareService
{
    private readonly IBarcodeLabelDocumentGenerator documentGenerator;
    private readonly IShare share;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeShareService"/> class.
    /// </summary>
    /// <param name="documentGenerator">The barcode PDF generator.</param>
    /// <param name="share">The device share service.</param>
    public BarcodeShareService(IBarcodeLabelDocumentGenerator documentGenerator, IShare share)
    {
        this.documentGenerator = documentGenerator ?? throw new ArgumentNullException(nameof(documentGenerator));
        this.share = share ?? throw new ArgumentNullException(nameof(share));
    }

    /// <inheritdoc />
    public async Task ShareAsync(string name, Barcode barcode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(barcode);

        await ShareAsync([new BarcodeLabelData(name, barcode.Value, barcode.Symbology)], $"Share barcode for {name}").ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ShareAsync(IReadOnlyCollection<BarcodeLabelData> labels, string title)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        if (labels.Count == 0)
        {
            throw new ArgumentException("At least one barcode label is required.", nameof(labels));
        }

        var fileName = $"mothball-barcodes-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}Z-{Guid.NewGuid():N}.pdf";
        var document = await documentGenerator.GenerateAsync(labels, fileName).ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
            share.RequestAsync(new ShareFileRequest(
                title,
                new ShareFile(document.FullPath))));
    }

}
