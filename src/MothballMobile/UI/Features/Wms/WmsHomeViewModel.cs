using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MothballMobile.UI.Features.Wms;

/// <summary>Provides the first mode-gated WMS receiving workflow.</summary>
public partial class WmsHomeViewModel : ObservableObject
{
    private readonly IApplicationSettings settings;
    private readonly IWorkspaceContext workspaceContext;
    private readonly ReceiveStockHandler receiveStock;

    public WmsHomeViewModel(IApplicationSettings settings, IWorkspaceContext workspaceContext, ReceiveStockHandler receiveStock)
    {
        this.settings = settings;
        this.workspaceContext = workspaceContext;
        this.receiveStock = receiveStock;
        settings.AppModeChanged += OnAppModeChanged;
    }

    public bool IsWmsEnabled => settings.IsWmsExperimentalMode;
    public bool IsWmsDisabled => !IsWmsEnabled;
    public bool IsNotBusy => !IsBusy;

    public string WmsTitle => LocalizationManager.Current.Get("Experimental WMS");
    public string WmsDescription => LocalizationManager.Current.Get("Receive stock into a canonical warehouse location.");
    public string DisabledMessage => LocalizationManager.Current.Get("Enable Experimental WMS in Settings to use this workspace.");
    public string ReceiveButtonText => LocalizationManager.Current.Get("Receive stock");
    public string ItemIdLabel => LocalizationManager.Current.Get("Item ID");
    public string LocationIdLabel => LocalizationManager.Current.Get("Location ID");
    public string QuantityLabel => LocalizationManager.Current.Get("Quantity");
    public string ReasonLabel => LocalizationManager.Current.Get("Reason");

    [ObservableProperty] private string itemId = string.Empty;
    [ObservableProperty] private string locationId = string.Empty;
    [ObservableProperty] private string quantity = "1";
    [ObservableProperty] private string reason = "WMS receipt";
    [ObservableProperty] private string status = string.Empty;
    [ObservableProperty] private bool isBusy;

    [RelayCommand]
    private async Task ReceiveAsync()
    {
        if (!IsWmsEnabled || IsBusy)
        {
            return;
        }

        if (!Guid.TryParse(ItemId, out var item) || !Guid.TryParse(LocationId, out var location) || !int.TryParse(Quantity, out var amount))
        {
            Status = LocalizationManager.Current.Get("Enter valid item, location, and quantity values.");
            return;
        }

        try
        {
            IsBusy = true;
            OnPropertyChanged(nameof(IsNotBusy));
            var (workspace, defaults) = await workspaceContext.EnsureDefaultAsync().ConfigureAwait(false);
            var destination = location == Guid.Empty ? defaults.UnassignedLocationId : location;
            var result = await receiveStock.HandleAsync(new ReceiveStockCommand(workspace.WorkspaceId, item, destination, amount, Reason)).ConfigureAwait(false);
            Status = LocalizationManager.Current.Format("Received {0} units. Operation: {1}", amount, result.OperationId);
        }
        catch (Exception ex)
        {
            Status = ex.Message;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    private void OnAppModeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(IsWmsEnabled));
        OnPropertyChanged(nameof(IsWmsDisabled));
        OnPropertyChanged(nameof(WmsTitle));
    }
}
