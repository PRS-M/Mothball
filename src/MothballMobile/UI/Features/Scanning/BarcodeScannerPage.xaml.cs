using CoreApp.Domain.Entities.Shared;
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
        if (result is null || !Enum.TryParse<BarcodeSymbology>(result.Format.ToString(), ignoreCase: true, out var symbology))
        {
            Interlocked.Exchange(ref hasAcceptedResult, 0);
            return;
        }

        if (BindingContext is BarcodeScannerViewModel viewModel)
        {
            await MainThread.InvokeOnMainThreadAsync(() => viewModel.CompleteAsync(new Barcode(result.Value, symbology)));
        }
    }

    private void OnTorchClicked(object? sender, EventArgs e)
    {
        barcodeReader.IsTorchOn = !barcodeReader.IsTorchOn;
    }

    private async void OnGalleryClicked(object? sender, EventArgs e)
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

        if (selected is not null && BindingContext is BarcodeScannerViewModel viewModel)
        {
            await viewModel.CompleteAsync(selected);
        }
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
        return Enum.TryParse<BarcodeSymbology>(result.Format.ToString(), ignoreCase: true, out var symbology)
            ? new Barcode(result.Value, symbology)
            : null;
    }
}