using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Handles sharing and importing the device's backup signing key.
/// </summary>
public partial class BackupSigningKeySettingsViewModel : SettingsSectionViewModelBase
{
    private readonly IBackupSigningKeyTransferService signingKeyTransfer;
    private readonly IFilePicker filePicker;
    private readonly IApplicationSettings applicationSettings;

    public BackupSigningKeySettingsViewModel(
        IBackupSigningKeyTransferService signingKeyTransfer,
        IFilePicker filePicker,
        IApplicationSettings applicationSettings,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ILogger<BackupSigningKeySettingsViewModel> logger)
        : base(popup, popupDefinitions, logger)
    {
        this.signingKeyTransfer = signingKeyTransfer ?? throw new ArgumentNullException(nameof(signingKeyTransfer));
        this.filePicker = filePicker;
        this.applicationSettings = applicationSettings;
    }

    public bool IsBackupSigningKeyEnabled
    {
        get => applicationSettings.IsBackupSigningKeyEnabled;
        set
        {
            if (applicationSettings.IsBackupSigningKeyEnabled == value)
            {
                return;
            }

            applicationSettings.IsBackupSigningKeyEnabled = value;
            OnPropertyChanged();
        }
    }

    [RelayCommand]
    private async Task ShareBackupSigningKeyAsync()
    {
        await RunCommandAsync(() => TryWithAlertAsync(
            () => signingKeyTransfer.ShareAsync(),
            "Failed to share the Mothball backup signing key.",
            PopupDefinitions.BackupSigningKeyShareFailed));
    }

    [RelayCommand]
    private async Task ImportBackupSigningKeyAsync()
    {
        await RunCommandAsync(async () =>
        {
            var file = await filePicker.PickAsync(new PickOptions
            {
                PickerTitle = Localization.Current.Get("Choose signing key"),
                FileTypes = JsonBackupFileType,
            });
            if (file is null)
            {
                return;
            }

            await TryWithAlertAsync(
                async () =>
                {
                    await using var stream = await file.OpenReadAsync();
                    var confirmed = await Popup.ConfirmAsync(
                        Localization.Current.Get("Import signing key"),
                        Localization.Current.Get("This replaces the current backup signing key on this device. Backups signed by the current key will no longer verify here."),
                        Localization.Current.Get("Import"),
                        Localization.Current.Get("Cancel"));
                    if (!confirmed)
                    {
                        return;
                    }

                    await signingKeyTransfer.ImportAsync(stream);
                    await Popup.ShowAlertAsync(new AlertPopupDefinition(
                        Localization.Current.Get("Signing key imported"),
                        Localization.Current.Get("This device can now verify backups signed by the imported key.")));
                },
                "Failed to import the Mothball backup signing key from {FileName}.",
                PopupDefinitions.BackupSigningKeyImportFailed,
                file.FileName);
        });
    }

    private static FilePickerFileType JsonBackupFileType => new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.iOS] = ["public.json"],
        [DevicePlatform.MacCatalyst] = ["public.json"],
        [DevicePlatform.Android] = ["application/json", "text/json", "text/plain"],
        [DevicePlatform.WinUI] = [".json"],
        [DevicePlatform.Tizen] = ["application/json", "text/json", "text/plain"],
    });
}
