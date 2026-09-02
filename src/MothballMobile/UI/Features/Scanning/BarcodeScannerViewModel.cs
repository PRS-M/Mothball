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

    public Task CompleteAsync(Barcode barcode)
        => scanner.CompleteAsync(barcode);

    [RelayCommand]
    private Task CancelAsync()
        => scanner.CompleteAsync(null);
}