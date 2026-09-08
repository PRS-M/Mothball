using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure.Presentation.Popups;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Provides power-user settings and navigation to photo-processing history.
/// </summary>
public partial class AdvancedSettingsViewModel : ObservableObject
{
    private readonly IApplicationSettings applicationSettings;
    private readonly INavigationService navigation;
    private readonly IInventoryMaintenanceService maintenance;
    private readonly IPopupService popup;
    public AdvancedSettingsViewModel(
        IApplicationSettings applicationSettings,
        BackupSigningKeySettingsViewModel signingKey,
        INavigationService navigation,
        IInventoryMaintenanceService maintenance,
        IPopupService popup)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
    }

    public BackupSigningKeySettingsViewModel SigningKey { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllPhotosCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetAllDataCommand))]
    private bool isMaintenanceInProgress;

    [ObservableProperty]
    private double maintenanceProgress;

    [ObservableProperty]
    private string maintenanceStatus = string.Empty;

    public bool IsMaintenanceIdle => !IsMaintenanceInProgress;

    public bool IsBarcodeExtendedMode
    {
        get => applicationSettings.IsBarcodeExtendedMode;
        set
        {
            if (applicationSettings.IsBarcodeExtendedMode == value)
            {
                return;
            }

            applicationSettings.IsBarcodeExtendedMode = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private Task NavigateToBackgroundOperationsAsync()
        => navigation.GoToAsync(NavigationRoutes.BackgroundOperations);

    [RelayCommand(CanExecute = nameof(IsMaintenanceIdle))]
    private async Task ReplaceAllPhotosAsync()
    {
        if (!await popup.ConfirmAsync(
                LocalizationManager.Current.Get("Replace all photos"),
                LocalizationManager.Current.Get("This will replace every inventory photo with generic shared images. Continue?"),
                LocalizationManager.Current.Get("Replace"),
                LocalizationManager.Current.Get("Cancel")))
        {
            return;
        }

        await RunMaintenanceAsync(maintenance.ReplaceAllPhotosWithSharedAssetsAsync);
    }

    [RelayCommand(CanExecute = nameof(IsMaintenanceIdle))]
    private async Task ResetAllDataAsync()
    {
        if (!await popup.ConfirmAsync(
                LocalizationManager.Current.Get("Delete all app data"),
                LocalizationManager.Current.Get("This will permanently delete all inventory data, assignments, barcodes, and photos. Continue?"),
                LocalizationManager.Current.Get("Delete all data"),
                LocalizationManager.Current.Get("Cancel")))
        {
            return;
        }

        await RunMaintenanceAsync(maintenance.ResetAllDataAsync);
    }

    private async Task RunMaintenanceAsync(Func<IProgress<MaintenanceProgress>, Task> operation)
    {
        IsMaintenanceInProgress = true;
        MaintenanceProgress = 0;
        MaintenanceStatus = LocalizationManager.Current.Get("Preparing");
        try
        {
            var progress = new Progress<MaintenanceProgress>(update =>
            {
                MaintenanceProgress = update.Progress;
                MaintenanceStatus = LocalizationManager.Current.Get(update.Status);
            });
            await operation(progress);
        }
        catch (Exception exception)
        {
            MaintenanceStatus = LocalizationManager.Current.Get("Something went wrong. Please try again.");
            await popup.ShowAlertAsync(
                LocalizationManager.Current.Get("Error"),
                exception.Message);
        }
        finally
        {
            IsMaintenanceInProgress = false;
        }
    }

    partial void OnIsMaintenanceInProgressChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMaintenanceIdle));
        ReplaceAllPhotosCommand.NotifyCanExecuteChanged();
        ResetAllDataCommand.NotifyCanExecuteChanged();
    }

}
