using CoreApp.Domain.Entities.Shared;
using Microsoft.Maui.Storage;
using SkiaSharp;
using ZXing.Net.Maui;

namespace MothballMobile.Infrastructure.BarcodeDocuments;

/// <summary>
/// Renders barcode labels into a fixed A4 PDF layout using SkiaSharp.
/// </summary>
public sealed class SkiaBarcodeLabelDocumentGenerator : IBarcodeLabelDocumentGenerator
{
    private const string DocumentsFolder = "BarcodeDocuments";

    private readonly IFileSystem fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkiaBarcodeLabelDocumentGenerator"/> class.
    /// </summary>
    /// <param name="fileSystem">The MAUI file-system provider.</param>
    public SkiaBarcodeLabelDocumentGenerator(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public async Task<BarcodeDocumentResult> GenerateAsync(
        IReadOnlyCollection<BarcodeLabelData> labels,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        if (labels.Count == 0)
        {
            throw new ArgumentException("At least one barcode label is required.", nameof(labels));
        }

        var directory = Path.Combine(fileSystem.AppDataDirectory, DocumentsFolder);
        Directory.CreateDirectory(directory);
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.Combine(directory, safeFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            ? safeFileName
            : $"{safeFileName}.pdf");

        var pageCount = BarcodeDocumentLayout.GetPageCount(labels.Count);
        await Task.Run(() => RenderAsync(labels, fullPath, pageCount, directory, cancellationToken), cancellationToken)
            .ConfigureAwait(false);

        return new BarcodeDocumentResult(Path.GetFileName(fullPath), fullPath, labels.Count, pageCount);
    }

    private static async Task RenderAsync(
        IReadOnlyCollection<BarcodeLabelData> labels,
        string fullPath,
        int pageCount,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        using var document = SKDocument.CreatePdf(fullPath);
        using var titleFont = new SKFont(SKTypeface.Default, 14);
        using var valueFont = new SKFont(SKTypeface.Default, 9);
        using var titlePaint = CreateTextPaint(SKColor.Parse("#1C1B1F"));
        using var valuePaint = CreateTextPaint(SKColor.Parse("#49454F"));
        using var borderPaint = new SKPaint
        {
            Color = SKColor.Parse("#79747E"),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            IsAntialias = true,
        };

        var labelIndex = 0;
        var labelList = labels.ToArray();

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var canvas = document.BeginPage(BarcodeDocumentLayout.PageWidth, BarcodeDocumentLayout.PageHeight);
            canvas.Clear(SKColors.White);

            for (var slotIndex = 0; slotIndex < BarcodeDocumentLayout.LabelsPerPage && labelIndex < labelList.Length; slotIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bounds = BarcodeDocumentLayout.GetBounds(slotIndex);
                await RenderLabelAsync(
                    canvas,
                    new SKRect(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom),
                    labelList[labelIndex++],
                    borderPaint,
                    titleFont,
                    titlePaint,
                    valueFont,
                    valuePaint,
                    temporaryDirectory,
                    cancellationToken).ConfigureAwait(false);
            }

            document.EndPage();
        }

        document.Close();
    }

    private static async Task RenderLabelAsync(
        SKCanvas canvas,
        SKRect bounds,
        BarcodeLabelData label,
        SKPaint borderPaint,
        SKFont titleFont,
        SKPaint titlePaint,
        SKFont valueFont,
        SKPaint valuePaint,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        canvas.DrawRoundRect(bounds, 6, 6, borderPaint);

        var content = new SKRect(bounds.Left + 10, bounds.Top + 10, bounds.Right - 10, bounds.Bottom - 10);
        canvas.DrawText(TrimToWidth(label.Name, titleFont, titlePaint, content.Width), content.Left, content.Top + titleFont.Size, SKTextAlign.Left, titleFont, titlePaint);

        cancellationToken.ThrowIfCancellationRequested();
        var temporaryPath = Path.Combine(temporaryDirectory, $"barcode-{Guid.NewGuid():N}.png");
        try
        {
            await BarcodeGenerator.WriteToFileAsync(
                label.BarcodeValue,
                temporaryPath,
                new BarcodeGeneratorOptions
                {
                    Format = ToBarcodeFormat(label.Symbology),
                    Width = Math.Max(240, (int)content.Width * 2),
                    Height = Math.Max(100, (int)content.Height / 2),
                    Margin = 2,
                    ForegroundColor = Colors.Black,
                    BackgroundColor = Colors.White,
                }).ConfigureAwait(false);

            using var bitmap = SKBitmap.Decode(temporaryPath)
            ?? throw new InvalidOperationException($"Could not render barcode '{label.BarcodeValue}'.");

            var imageTop = content.Top + 24;
            var imageBottom = content.Bottom - 26;
            var imageRect = FitRect(bitmap.Width, bitmap.Height, new SKRect(content.Left, imageTop, content.Right, imageBottom));
            canvas.DrawBitmap(bitmap, imageRect);

            var value = $"{label.BarcodeValue} ({label.Symbology})";
            canvas.DrawText(TrimToWidth(value, valueFont, valuePaint, content.Width), content.Left, content.Bottom - 4, SKTextAlign.Left, valueFont, valuePaint);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static BarcodeFormat ToBarcodeFormat(BarcodeSymbology symbology)
        => symbology switch
        {
            BarcodeSymbology.QrCode => BarcodeFormat.QrCode,
            BarcodeSymbology.Aztec => BarcodeFormat.Aztec,
            BarcodeSymbology.Codabar => BarcodeFormat.Codabar,
            BarcodeSymbology.Code39 => BarcodeFormat.Code39,
            BarcodeSymbology.Code93 => BarcodeFormat.Code93,
            BarcodeSymbology.Code128 => BarcodeFormat.Code128,
            BarcodeSymbology.DataMatrix => BarcodeFormat.DataMatrix,
            BarcodeSymbology.Ean8 => BarcodeFormat.Ean8,
            BarcodeSymbology.Ean13 => BarcodeFormat.Ean13,
            BarcodeSymbology.Itf => BarcodeFormat.Itf,
            BarcodeSymbology.Pdf417 => BarcodeFormat.Pdf417,
            BarcodeSymbology.UpcA => BarcodeFormat.UpcA,
            BarcodeSymbology.UpcE => BarcodeFormat.UpcE,
            _ => throw new ArgumentOutOfRangeException(nameof(symbology), symbology, "Unsupported barcode symbology."),
        };

    private static SKRect FitRect(int width, int height, SKRect bounds)
    {
        var scale = Math.Min(bounds.Width / width, bounds.Height / height);
        var drawWidth = width * scale;
        var drawHeight = height * scale;
        var left = bounds.MidX - drawWidth / 2;
        var top = bounds.MidY - drawHeight / 2;
        return new SKRect(left, top, left + drawWidth, top + drawHeight);
    }

    private static string TrimToWidth(string value, SKFont font, SKPaint paint, float width)
    {
        if (font.MeasureText(value, paint) <= width)
        {
            return value;
        }

        const string suffix = "…";
        var result = value;
        while (result.Length > 1 && font.MeasureText(result + suffix, paint) > width)
        {
            result = result[..^1];
        }

        return result + suffix;
    }

    private static SKPaint CreateTextPaint(SKColor color)
        => new()
        {
            Color = color,
            IsAntialias = true,
        };
}
