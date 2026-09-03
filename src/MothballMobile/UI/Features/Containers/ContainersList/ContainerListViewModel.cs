using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Application.Utilities;
using MothballMobile.Infrastructure.Scanning;
using MothballMobile.Infrastructure.BarcodeDocuments;

namespace MothballMobile.UI.Features.Containers.ContainersList;

public enum ContainerListFilter
{
    All,
    Empty,
}

public partial class ContainerListViewModel : SearchablePagedListViewModelBase<Container, ContainerViewModel>
{
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService nav;
    private readonly IContainerListQueryHandler containerListQueries;
    private readonly IApplicationSettings applicationSettings;
    private readonly IInventoryChangeTracker inventoryChanges;
    private readonly BarcodeLookupCoordinator barcodeLookup;
    private readonly IBarcodeShareService? barcodeShare;

    private ContainerListFilter selectedFilter = ContainerListFilter.All;

    [ObservableProperty]
    private bool isSelectionMode;

    public static ReadOnlyCollection<ContainerListFilter> AvailableFilters { get; } = EnumValues.CreateReadOnly<ContainerListFilter>();

    public ContainerListFilter SelectedFilter
    {
        get => selectedFilter;
        set
        {
            if (!SetProperty(ref selectedFilter, value))
            {
                return;
            }

            MainThread.InvokeOnMainThreadAsync(SearchAsync)
                .FireAndForget(backgroundTasks, SearchOperationName);
        }
    }

    public ContainerListViewModel(
        IImagePathResolver imagePaths,
        IContainerListQueryHandler containerListQueries,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IInventoryChangeTracker inventoryChanges,
        BarcodeLookupCoordinator barcodeLookup,
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer = null,
        IPagedListLoadDiagnostics? loadDiagnostics = null,
        IBarcodeShareService? barcodeShare = null)
        : base(backgroundTasks, debouncer, pageSize: 10, loadDiagnostics: loadDiagnostics)
    {
        this.imagePaths = imagePaths;
        this.containerListQueries = containerListQueries;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.inventoryChanges = inventoryChanges;
        this.barcodeLookup = barcodeLookup ?? throw new ArgumentNullException(nameof(barcodeLookup));
        this.barcodeShare = barcodeShare;
    }

    protected override string SearchOperationName => "Search containers";
    protected override long DataRevision => inventoryChanges.Revision;
    protected override string LoadVariant => $"{SelectedFilter}:{base.LoadVariant}";

    public ObservableCollection<ContainerViewModel> Containers => Items;

    public int SelectedCount => Containers.Count(container => container.IsSelected);
    public bool HasSelection => SelectedCount > 0;

    protected override Task<List<Container>> LoadPageAsync(string? query, int pageNumber, int pageSize)
        => containerListQueries.QueryAsync(
            IsEmptyFilterSelected(),
            query,
            pageNumber,
            pageSize);

    protected override ContainerViewModel MapToViewModel(Container source)
        => new ContainerViewModel(source, imagePaths, nav, applicationSettings.IsAdvancedMode);

    protected override void OnViewModelAdded(ContainerViewModel vm)
    {
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ContainerViewModel.IsSelected))
            {
                OnPropertyChanged(nameof(SelectedCount));
                OnPropertyChanged(nameof(HasSelection));
            }
        };
    }

    [RelayCommand]
    private Task NavigateToAddContainerAsync() => nav.GoToAsync(NavigationRoutes.AddContainer);

    [RelayCommand]
    private Task ScanToFindAsync() => RunCommandAsync(barcodeLookup.ScanAndNavigateAsync, rethrowOnError: false);

    [RelayCommand]
    private void EnterSelectionMode() => IsSelectionMode = true;

    [RelayCommand]
    private void SelectAllLoaded()
    {
        foreach (var container in Containers)
        {
            container.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var container in Containers)
        {
            container.IsSelected = false;
        }
    }

    [RelayCommand]
    private void CancelSelection()
    {
        ClearSelection();
        IsSelectionMode = false;
    }

    [RelayCommand]
    private Task ShareSelectedAsync()
    {
        var labels = Containers
            .Where(container => container.IsSelected && container.Container.Barcode is not null)
            .Select(container => new BarcodeLabelData(
                container.Name,
                container.Container.Barcode!.Value,
                container.Container.Barcode.Symbology))
            .ToArray();

        if (labels.Length == 0)
        {
            return RunCommandAsync(
                () => Task.FromException(new InvalidOperationException(LocalizationManager.Current.Get("No barcode labels found."))),
                rethrowOnError: false);
        }

        return barcodeShare is null
            ? Task.CompletedTask
            : RunCommandAsync(
                () => barcodeShare.ShareAsync(labels, "Share container barcodes"),
                rethrowOnError: false);
    }

    [RelayCommand]
    private Task ShareAllMatchingAsync()
        => RunCommandAsync(async () =>
        {
            var containers = await containerListQueries.QueryAsync(IsEmptyFilterSelected(), Query, null, null);
            var labels = containers.Where(container => container.Barcode is not null)
                .Select(container => new BarcodeLabelData(container.Name, container.Barcode!.Value, container.Barcode.Symbology))
                .ToArray();
            if (labels.Length == 0)
            {
                throw new InvalidOperationException(LocalizationManager.Current.Get("No barcode labels found."));
            }

            if (barcodeShare is not null)
            {
                await barcodeShare.ShareAsync(labels, "Share container barcodes");
            }
        }, rethrowOnError: false);

    private bool IsEmptyFilterSelected()
        => SelectedFilter == ContainerListFilter.Empty;

}
