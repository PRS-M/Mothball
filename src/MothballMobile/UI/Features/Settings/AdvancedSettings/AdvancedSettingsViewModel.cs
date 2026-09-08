using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure.BarcodeDocuments;
using MothballMobile.Infrastructure.Presentation.Popups;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Provides power-user settings and navigation to photo-processing history.
/// </summary>
public partial class AdvancedSettingsViewModel : ObservableObject
{
    private readonly IApplicationSettings applicationSettings;
    private readonly INavigationService navigation;
    private readonly IBarcodeShareService? barcodeShare;
    private readonly IPopupService? popup;

    public AdvancedSettingsViewModel(
        IApplicationSettings applicationSettings,
        BackupSigningKeySettingsViewModel signingKey,
        INavigationService navigation,
        IBarcodeShareService? barcodeShare = null,
        IPopupService? popup = null)
    {
        this.applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
        SigningKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.barcodeShare = barcodeShare;
        this.popup = popup;
    }

    public BackupSigningKeySettingsViewModel SigningKey { get; }

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

    [RelayCommand]
    private async Task GenerateNewSkuPdfAsync()
    {
        if (barcodeShare is null || popup is null)
        {
            return;
        }

        var count = await popup.PickNumberAsync(
            LocalizationManager.Current.Get("Generate PDF with new codes"),
            min: 1,
            max: 1000,
            initialValue: 10,
            accept: LocalizationManager.Current.Get("Generate"),
            cancel: LocalizationManager.Current.Get("Cancel"));
        if (count is null)
        {
            return;
        }

        await barcodeShare.ShareNewInternalSkuBatchAsync(
            count.Value,
            LocalizationManager.Current.Get("Internal SKU"),
            LocalizationManager.Current.Get("Generate PDF with new codes"));
    }
}
