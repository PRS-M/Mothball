using System.Globalization;
using CoreApp.Domain.ValueObjects;
using ZXing.Net.Maui;

namespace MothballMobile.UI.Features.Scanning;

public partial class BarcodeScannerPage
{
    private int hasAcceptedResult;

    public BarcodeScannerPage(BarcodeScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        barcodeReader.IsVisible = BarcodeScanning.IsSupported;
        cameraUnsupportedMessage.IsVisible = !BarcodeScanning.IsSupported;
        barcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false,
            TryHarder = true,
        };
    }

    private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (Interlocked.Exchange(ref hasAcceptedResult, 1) != 0)
        {
            return;
        }

        var result = e.Results.FirstOrDefault();
        if (result is null
            || !BarcodeFormatMapper.TryToBarcodeSymbology(result.Format, out var symbology)
            || BindingContext is not BarcodeScannerViewModel viewModel
            || !viewModel.IsSymbologyAllowed(symbology))
        {
            Interlocked.Exchange(ref hasAcceptedResult, 0);
            return;
        }

        await MainThread.InvokeOnMainThreadAsync(() => viewModel.CompleteAsync(new Barcode(result.Value, symbology)));
    }

    private void OnTorchClicked(object? sender, EventArgs e)
    {
        barcodeReader.IsTorchOn = !barcodeReader.IsTorchOn;
    }

    private async void OnGalleryClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not BarcodeScannerViewModel viewModel)
        {
            return;
        }

        await viewModel.ProcessGalleryAsync(async () =>
        {
            var file = (await MediaPicker.PickPhotosAsync()).FirstOrDefault();
            if (file is null)
            {
                return;
            }

            await using var stream = await file.OpenReadAsync();
            var results = await BarcodeReader.DecodeAsync(stream, new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.All,
                AutoRotate = true,
                Multiple = true,
                TryHarder = true,
            });

            var barcodes = results?
                .Select(result => TryCreateBarcode(result))
                .Where(barcode => barcode is not null)
                .Cast<Barcode>()
                .Where(barcode => viewModel.IsSymbologyAllowed(barcode.Symbology))
                .Distinct()
                .ToArray() ?? [];

            if (barcodes.Length == 0)
            {
                await DisplayAlertAsync(
                    LocalizationManager.Current.Get("NoBarcodeFound"),
                    LocalizationManager.Current.Get("NoSupportedBarcodeInImage"),
                    LocalizationManager.Current.Get("OK"));
                return;
            }

            var selected = barcodes.Length == 1
                ? barcodes[0]
                : await SelectBarcodeAsync(barcodes);

            if (selected is not null)
            {
                await viewModel.CompleteAsync(selected);
            }
        });
    }

    private async Task<Barcode?> SelectBarcodeAsync(IReadOnlyList<Barcode> barcodes)
    {
        var options = barcodes.Select(barcode => $"{barcode.Symbology}: {barcode.Value}").ToArray();
        var selected = await DisplayActionSheetAsync(
            LocalizationManager.Current.Get("SelectBarcode"),
            LocalizationManager.Current.Get("Cancel"),
            null,
            options);
        var index = Array.IndexOf(options, selected);
        return index >= 0 ? barcodes[index] : null;
    }

    private static Barcode? TryCreateBarcode(BarcodeResult result)
    {
        return BarcodeFormatMapper.TryToBarcodeSymbology(result.Format, out var symbology)
            ? new Barcode(result.Value, symbology)
            : null;
    }
}


/// <summary>
/// Converts a displayed barcode symbology name to the typed ZXing format required by its generator control.
/// </summary>
public sealed class BarcodeFormatConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string symbologyName
           && Enum.TryParse<BarcodeSymbology>(symbologyName, ignoreCase: false, out var symbology)
            ? BarcodeFormatMapper.ToBarcodeFormat(symbology)
            : BarcodeFormatMapper.ToBarcodeFormat(BarcodeSymbology.QrCode);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

internal static class BarcodeFormatMapper
{
    public static BarcodeFormat ToBarcodeFormat(BarcodeSymbology symbology)
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

    public static bool TryToBarcodeSymbology(BarcodeFormat format, out BarcodeSymbology symbology)
    {
        switch (format)
        {
            case BarcodeFormat.QrCode:
                symbology = BarcodeSymbology.QrCode;
                return true;
            case BarcodeFormat.Aztec:
                symbology = BarcodeSymbology.Aztec;
                return true;
            case BarcodeFormat.Codabar:
                symbology = BarcodeSymbology.Codabar;
                return true;
            case BarcodeFormat.Code39:
                symbology = BarcodeSymbology.Code39;
                return true;
            case BarcodeFormat.Code93:
                symbology = BarcodeSymbology.Code93;
                return true;
            case BarcodeFormat.Code128:
                symbology = BarcodeSymbology.Code128;
                return true;
            case BarcodeFormat.DataMatrix:
                symbology = BarcodeSymbology.DataMatrix;
                return true;
            case BarcodeFormat.Ean8:
                symbology = BarcodeSymbology.Ean8;
                return true;
            case BarcodeFormat.Ean13:
                symbology = BarcodeSymbology.Ean13;
                return true;
            case BarcodeFormat.Itf:
                symbology = BarcodeSymbology.Itf;
                return true;
            case BarcodeFormat.Pdf417:
                symbology = BarcodeSymbology.Pdf417;
                return true;
            case BarcodeFormat.UpcA:
                symbology = BarcodeSymbology.UpcA;
                return true;
            case BarcodeFormat.UpcE:
                symbology = BarcodeSymbology.UpcE;
                return true;
            default:
                symbology = default;
                return false;
        }
    }
}