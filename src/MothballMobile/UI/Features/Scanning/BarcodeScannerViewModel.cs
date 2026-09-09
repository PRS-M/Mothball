using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.ValueObjects;
using MothballMobile.Infrastructure.Scanning;

namespace MothballMobile.UI.Features.Scanning;

public sealed partial class BarcodeScannerViewModel : BaseViewModel
{
    private readonly IBarcodeScanSession scanner;

    public BarcodeScannerViewModel(
        IBarcodeScanSession scanner)
    {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    /// <summary>
    /// Determines whether a decoded barcode type is available in the current mode.
    /// </summary>
    /// <param name="symbology">The decoded barcode type.</param>
    /// <returns><see langword="true"/> when the scanner can decode the symbology.</returns>
    public bool IsSymbologyAllowed(BarcodeSymbology symbology)
        => Enum.IsDefined(symbology);

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

    /// <summary>
    /// Cancels the pending scan when the scanner page disappears unexpectedly.
    /// </summary>
    public Task CancelPendingScanAsync()
        => scanner.CancelAsync();

    [RelayCommand]
    private Task CancelAsync()
        => RunCommandAsync(() => scanner.CompleteAsync(null), rethrowOnError: false);
}
