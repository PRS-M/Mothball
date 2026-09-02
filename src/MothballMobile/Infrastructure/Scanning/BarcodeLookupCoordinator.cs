using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts;
using MothballMobile.Infrastructure.Navigation;

namespace MothballMobile.Infrastructure.Scanning;

/// <summary>
/// Scans a barcode and navigates to the container or item that owns it.
/// </summary>
public sealed class BarcodeLookupCoordinator
{
    private readonly IBarcodeScanSession scanner;
    private readonly IInventoryQueryRepository inventoryQueries;
    private readonly INavigationService navigation;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarcodeLookupCoordinator"/> class.
    /// </summary>
    /// <param name="scanner">The service that acquires a barcode.</param>
    /// <param name="inventoryQueries">The inventory query repository used to resolve barcode owners.</param>
    /// <param name="navigation">The navigation service used to display the matching details page.</param>
    public BarcodeLookupCoordinator(
        IBarcodeScanSession scanner,
        IInventoryQueryRepository inventoryQueries,
        INavigationService navigation)
    {
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        this.inventoryQueries = inventoryQueries ?? throw new ArgumentNullException(nameof(inventoryQueries));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    /// <summary>
    /// Scans for a barcode and navigates to its owner when one exists.
    /// </summary>
    /// <returns><see langword="true"/> when an owner was found and opened; otherwise <see langword="false"/>.</returns>
    public async Task<bool> ScanAndNavigateAsync()
    {
        var barcode = await scanner.ScanAsync();
        if (barcode is null)
        {
            return false;
        }

        var owner = await inventoryQueries.FindBarcodeAsync(barcode.Value);
        if (owner is null)
        {
            return false;
        }

        switch (owner.OwnerKind)
        {
            case BarcodeOwnerKind.Container:
                await navigation.GoToAsync(
                    NavigationRoutes.ContainerDetails,
                    new ContainerDetailsNavigationRequest(owner.OwnerId));
                return true;
            case BarcodeOwnerKind.Item:
                await navigation.GoToAsync(
                    NavigationRoutes.ItemDetails,
                    new ItemDetailsNavigationRequest(owner.OwnerId));
                return true;
            default:
                throw new InvalidOperationException($"Unsupported barcode owner kind: {owner.OwnerKind}.");
        }
    }
}