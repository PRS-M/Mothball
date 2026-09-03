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

        var fileName = $"mothball-barcode-{Slugify(name)}-{Guid.NewGuid():N}.pdf";
        var document = await documentGenerator.GenerateAsync(
            [new BarcodeLabelData(name, barcode.Value, barcode.Symbology)],
            fileName).ConfigureAwait(false);

        await MainThread.InvokeOnMainThreadAsync(() =>
            share.RequestAsync(new ShareFileRequest(
                $"Share barcode for {name}",
                new ShareFile(document.FullPath))));
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();
        var slug = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "label" : slug.ToLowerInvariant();
    }
}
