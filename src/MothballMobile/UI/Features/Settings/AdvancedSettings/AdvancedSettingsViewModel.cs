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
    private readonly DemoDataSeeder? demoSeeder;
    public AdvancedSettingsViewModel(
        IApplicationSettings applicationSettings,
        BackupSigningKeySettingsViewModel signingKey,
        INavigationService navigation,
        IInventoryMaintenanceService maintenance,
        IPopupService popup,
        DemoDataSeeder? demoSeeder = null)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.maintenance = maintenance ?? throw new ArgumentNullException(nameof(maintenance));
        this.popup = popup ?? throw new ArgumentNullException(nameof(popup));
        this.demoSeeder = demoSeeder;
    }

    public BackupSigningKeySettingsViewModel SigningKey { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ReplaceAllPhotosCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetAllDataCommand))]
    private bool isMaintenanceInProgress;

    [ObservableProperty]
    private double maintenanceProgress;

    [ObservableProperty]
    private double maintenanceStepProgress;

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

        if (!await RunMaintenanceAsync(maintenance.ResetAllDataAsync) || demoSeeder is null)
        {
            return;
        }

        if (!await popup.ConfirmAsync(
                LocalizationManager.Current.Get("Seed demo data"),
                LocalizationManager.Current.Get("The inventory store is empty. Create the demonstration containers, items, and tags now?"),
                LocalizationManager.Current.Get("Seed"),
                LocalizationManager.Current.Get("Skip")))
        {
            return;
        }

        await SeedDemoDataAsync();
    }

    private async Task<bool> RunMaintenanceAsync(Func<IProgress<MaintenanceProgress>, Task> operation)
    {
        IsMaintenanceInProgress = true;
        MaintenanceProgress = 0;
        MaintenanceStepProgress = 0;
        MaintenanceStatus = LocalizationManager.Current.Get("Preparing");
        var succeeded = false;
        try
        {
            var progress = new Progress<MaintenanceProgress>(update =>
            {
                MaintenanceProgress = update.Progress;
                MaintenanceStatus = LocalizationManager.Current.Get(update.Status);
            });
            await operation(progress);
            succeeded = true;
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

        return succeeded;
    }

    private async Task SeedDemoDataAsync()
    {
        IsMaintenanceInProgress = true;
        MaintenanceProgress = 0;
        MaintenanceStepProgress = 0;
        try
        {
            var containerProgress = new Progress<double>(fraction =>
            {
                MaintenanceProgress = 0.5 * fraction;
                MaintenanceStepProgress = fraction;
                MaintenanceStatus = LocalizationManager.Current.Get("Generating demo containers");
            });
            await demoSeeder!.EnsureContainersAsync(100, withPhotos: true, containerProgress);

            var itemProgress = new Progress<double>(fraction =>
            {
                MaintenanceProgress = 0.5 + 0.5 * fraction;
                MaintenanceStepProgress = fraction;
                MaintenanceStatus = LocalizationManager.Current.Get("Generating demo items");
            });
            await demoSeeder.EnsureItemsAsync(100, withPhotos: true, itemProgress);
            MaintenanceProgress = 1;
            MaintenanceStepProgress = 1;
            MaintenanceStatus = LocalizationManager.Current.Get("Demo data ready");
        }
        catch (Exception exception)
        {
            MaintenanceStatus = LocalizationManager.Current.Get("Something went wrong. Please try again.");
            await popup.ShowAlertAsync(LocalizationManager.Current.Get("Error"), exception.Message);
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
