using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.Shared;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Scanning;

public sealed partial class BarcodeScannerViewModel : BaseViewModel
{
    private readonly IBarcodeScanSession scanner;

    public BarcodeScannerViewModel(IBarcodeScanSession scanner)
    {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    }

    /// <summary>
    /// Completes the active scan while exposing processing state to the scanner page.
    /// </summary>
    /// <param name="barcode">The barcode selected by the user.</param>
    /// <returns>A task that completes after the scanner closes.</returns>
    public Task CompleteAsync(Barcode barcode)
        => IsBusy ? scanner.CompleteAsync(barcode) : RunCommandAsync(() => scanner.CompleteAsync(barcode));

    /// <summary>
    /// Runs gallery barcode processing while exposing processing state to the scanner page.
    /// </summary>
    /// <param name="operation">The gallery operation to run.</param>
    /// <returns>A task that completes after the gallery operation finishes.</returns>
    public Task ProcessGalleryAsync(Func<Task> operation)
        => RunCommandAsync(operation);

    [RelayCommand]
    private Task CancelAsync()
        => RunCommandAsync(() => scanner.CompleteAsync(null));
}