using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.ValueObjects;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.Settings;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Scanning;

public sealed partial class BarcodeScannerViewModel : BaseViewModel
{
    private readonly IBarcodeScanSession scanner;
    private readonly IApplicationSettings applicationSettings;

    public BarcodeScannerViewModel(
        IBarcodeScanSession scanner,
        IApplicationSettings applicationSettings)
    {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
    }

    /// <summary>
    /// Determines whether a decoded barcode type is available in the current mode.
    /// </summary>
    /// <param name="symbology">The decoded barcode type.</param>
    /// <returns><see langword="true"/> for QR codes and for every type in extended mode; otherwise <see langword="false"/>.</returns>
    public bool IsSymbologyAllowed(BarcodeSymbology symbology)
        => applicationSettings.IsBarcodeExtendedMode || symbology == BarcodeSymbology.QrCode;

    /// <summary>
    /// Completes the active scan while exposing processing state to the scanner page.
    /// </summary>
    /// <param name="barcode">The barcode selected by the user.</param>
    /// <returns>A task that completes after the scanner closes.</returns>
    public Task CompleteAsync(Barcode barcode)
        => IsBusy ? scanner.CompleteAsync(barcode) : RunCommandAsync(() => scanner.CompleteAsync(barcode), rethrowOnError: false);

    /// <summary>
    /// Runs gallery barcode processing while exposing processing state to the scanner page.
    /// </summary>
    /// <param name="operation">The gallery operation to run.</param>
    /// <returns>A task that completes after the gallery operation finishes.</returns>
    public Task ProcessGalleryAsync(Func<Task> operation)
        => RunCommandAsync(operation, rethrowOnError: false);

    [RelayCommand]
    private Task CancelAsync()
        => RunCommandAsync(() => scanner.CompleteAsync(null), rethrowOnError: false);
}