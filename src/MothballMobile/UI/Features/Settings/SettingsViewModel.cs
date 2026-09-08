using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MothballMobile.Infrastructure.BarcodeDocuments;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Composes the settings page from its independent appearance, backup, and signing-key sections.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly INavigationService nav;
    private readonly IBarcodeShareService? barcodeShare;
    private readonly IPopupService? popup;

    public SettingsViewModel(
        AppearanceSettingsViewModel appearance,
        BackupSettingsViewModel backup,
        INavigationService nav,
        IBarcodeShareService? barcodeShare = null,
        IPopupService? popup = null)
    {
        Appearance = appearance;
        Backup = backup;
        this.nav = nav;
        this.barcodeShare = barcodeShare;
        this.popup = popup;
    }

    public AppearanceSettingsViewModel Appearance { get; }

    public BackupSettingsViewModel Backup { get; }

    [RelayCommand]
    private Task NavigateToAdvancedSettingsAsync()
        => nav.GoToAsync(NavigationRoutes.AdvancedSettings);

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
