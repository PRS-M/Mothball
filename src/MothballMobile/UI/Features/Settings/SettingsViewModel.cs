using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IInventoryBackupWorkflowService backupWorkflows;
    private readonly IBackupSigningKeyTransferService signingKeyTransfer;
    private readonly IFilePicker filePicker;
    private readonly INavigationService nav;
    private readonly IApplicationSettings applicationSettings;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly ILogger<SettingsViewModel> logger;
    private bool isZipBackupMode;

    public IReadOnlyList<string> ThemeOptions { get; } =
    [
        "Auto (System)",
        "Light",
        "Dark",
    ];

    public SettingsViewModel(
        IInventoryBackupWorkflowService backupWorkflows,
        IBackupSigningKeyTransferService signingKeyTransfer,
        IFilePicker filePicker,
        INavigationService nav,
        IApplicationSettings applicationSettings,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ILogger<SettingsViewModel> logger)
    {
        this.backupWorkflows = backupWorkflows ?? throw new ArgumentNullException(nameof(backupWorkflows));
        this.signingKeyTransfer = signingKeyTransfer ?? throw new ArgumentNullException(nameof(signingKeyTransfer));
        this.filePicker = filePicker;
        this.nav = nav;
        this.applicationSettings = applicationSettings;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool IsZipBackupMode
    {
        get => isZipBackupMode;
        set
        {
            if (SetProperty(ref isZipBackupMode, value))
            {
                OnPropertyChanged(nameof(IsJsonBackupMode));
            }
        }
    }

    public bool IsJsonBackupMode => !IsZipBackupMode;

    public string SelectedThemeOption
    {
        get => applicationSettings.ThemeOverride switch
        {
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            _ => "Auto (System)",
        };
        set
        {
            var theme = value switch
            {
                "Light" => AppTheme.Light,
                "Dark" => AppTheme.Dark,
                _ => AppTheme.Unspecified,
            };

            if (applicationSettings.ThemeOverride == theme)
            {
                return;
            }

            applicationSettings.ThemeOverride = theme;
            Application.Current!.UserAppTheme = theme;
            OnPropertyChanged();
        }
    }

    public bool IsAdvancedAppMode
    {
        get => applicationSettings.IsAdvancedMode;
        set
        {
            if (applicationSettings.IsAdvancedMode == value)
            {
                return;
            }

            applicationSettings.IsAdvancedMode = value;
            OnPropertyChanged();
        }
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
    private Task NavigateToBackgroundOperationsAsync()
        => nav.GoToAsync(NavigationRoutes.BackgroundOperations);

    [RelayCommand]
    private Task NavigateToImportDocumentationAsync()
        => nav.GoToAsync(NavigationRoutes.ImportDocumentation);

    [RelayCommand]
    private async Task ExportToJsonAsync()
    {
        await RunCommandAsync(async () =>
        {
            try
            {
                var export = await backupWorkflows.ExportJsonAsync();
                await popup.ShowAlertAsync(popupDefinitions.BackupExported(export.FullPath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export inventory backup to JSON.");
                await popup.ShowAlertAsync(popupDefinitions.BackupExportFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ExportToZipAsync()
    {
        await RunCommandAsync(async () =>
        {
            try
            {
                var export = await backupWorkflows.ExportZipAsync();
                await popup.ShowAlertAsync(popupDefinitions.BackupExported(export.FullPath));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to export inventory backup to ZIP.");
                await popup.ShowAlertAsync(popupDefinitions.BackupExportFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ImportFromJsonAsync()
    {
        await RunCommandAsync(async () =>
        {
            var policy = await SelectRestorePolicyAsync();
            if (policy is null)
                return;

            var fileName = await SelectBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                var backupJson = await backupWorkflows.ReadJsonAsync(fileName);
                await RestoreJsonAsync(backupJson, policy.Value, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import inventory backup from JSON file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.RestoreFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ImportJsonFromFileSystemAsync()
    {
        await RunCommandAsync(async () =>
        {
            var policy = await SelectRestorePolicyAsync();
            if (policy is null)
                return;

            var file = await PickBackupFileAsync("Choose JSON backup", JsonBackupFileType);
            if (file is null)
                return;

            try
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var backupJson = await reader.ReadToEndAsync();

                await RestoreJsonAsync(backupJson, policy.Value, file.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import inventory backup from external JSON file {FileName}.", file.FileName);
                await popup.ShowAlertAsync(popupDefinitions.RestoreFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ShareJsonAsync()
    {
        await RunCommandAsync(async () =>
        {
            var fileName = await SelectBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            await ShareBackupFileAsync(fileName, "Share JSON backup");
        });
    }

    [RelayCommand]
    private async Task ImportFromZipAsync()
    {
        await RunCommandAsync(async () =>
        {
            var policy = await SelectRestorePolicyAsync();
            if (policy is null)
                return;

            var fileName = await SelectZipBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            try
            {
                var backupZip = await backupWorkflows.ReadZipAsync(fileName);
                await RestoreZipAsync(backupZip, policy.Value, fileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import inventory backup from ZIP file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.RestoreFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ImportZipFromFileSystemAsync()
    {
        await RunCommandAsync(async () =>
        {
            var policy = await SelectRestorePolicyAsync();
            if (policy is null)
                return;

            var file = await PickBackupFileAsync("Choose ZIP backup", ZipBackupFileType);
            if (file is null)
                return;

            try
            {
                await using var stream = await file.OpenReadAsync();
                using var memory = new MemoryStream();
                await stream.CopyToAsync(memory);

                await RestoreZipAsync(memory.ToArray(), policy.Value, file.FileName);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import inventory backup from external ZIP file {FileName}.", file.FileName);
                await popup.ShowAlertAsync(popupDefinitions.RestoreFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ShareZipAsync()
    {
        await RunCommandAsync(async () =>
        {
            var fileName = await SelectZipBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            await ShareBackupFileAsync(fileName, "Share ZIP backup");
        });
    }

    [RelayCommand]
    private async Task ShareBackupSigningKeyAsync()
    {
        await RunCommandAsync(async () =>
        {
            try
            {
                await signingKeyTransfer.ShareAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to share the Mothball backup signing key.");
                await popup.ShowAlertAsync(popupDefinitions.BackupSigningKeyShareFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task ImportBackupSigningKeyAsync()
    {
        await RunCommandAsync(async () =>
        {
            var file = await PickBackupSigningKeyFileAsync();
            if (file is null)
            {
                return;
            }

            try
            {
                await using var stream = await file.OpenReadAsync();
                var confirmed = await popup.ConfirmAsync(
                    "Import signing key",
                    "This replaces the current backup signing key on this device. Backups signed by the current key will no longer verify here.",
                    "Import",
                    "Cancel");
                if (!confirmed)
                {
                    return;
                }

                await signingKeyTransfer.ImportAsync(stream);
                await popup.ShowAlertAsync(new AlertPopupDefinition(
                    "Signing key imported",
                    "This device can now verify backups signed by the imported key."));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import the Mothball backup signing key from {FileName}.", file.FileName);
                await popup.ShowAlertAsync(popupDefinitions.BackupSigningKeyImportFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task DeleteJsonAsync()
    {
        await RunCommandAsync(async () =>
        {
            var fileName = await SelectBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var confirmed = await popup.ConfirmAsync(popupDefinitions.DeleteBackup(fileName));

            if (!confirmed)
                return;

            try
            {
                await backupWorkflows.DeleteAsync(fileName);
                await popup.ShowAlertAsync(popupDefinitions.BackupDeleted(fileName));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete inventory backup JSON file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.DeleteBackupFailed(ex.Message));
            }
        });
    }

    [RelayCommand]
    private async Task DeleteZipAsync()
    {
        await RunCommandAsync(async () =>
        {
            var fileName = await SelectZipBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            var confirmed = await popup.ConfirmAsync(popupDefinitions.DeleteBackup(fileName));

            if (!confirmed)
                return;

            try
            {
                await backupWorkflows.DeleteAsync(fileName);
                await popup.ShowAlertAsync(popupDefinitions.BackupDeleted(fileName));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete inventory backup ZIP file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.DeleteBackupFailed(ex.Message));
            }
        });
    }

    private async Task<InventoryBackupConflictPolicy?> SelectRestorePolicyAsync()
        => await popup.SelectValueOptionAsync(popupDefinitions.RestorePolicyPicker());

    private async Task RestoreJsonAsync(
        string backupJson,
        InventoryBackupConflictPolicy policy,
        string fileName)
    {
        var result = await backupWorkflows.RestoreJsonAsync(backupJson, policy);

        await popup.ShowAlertAsync(popupDefinitions.RestoreCompleted(BuildRestoreSummary(result, policy, fileName)));
    }

    private async Task RestoreZipAsync(
        byte[] backupZip,
        InventoryBackupConflictPolicy policy,
        string fileName)
    {
        var restore = await backupWorkflows.RestoreZipAsync(backupZip, policy);

        await popup.ShowAlertAsync(popupDefinitions.RestoreCompleted(BuildRestoreSummary(restore.Result, policy, fileName, restore.RestoredPhotoFiles)));
    }

    private async Task ShareBackupFileAsync(string fileName, string title)
    {
        try
        {
            await backupWorkflows.ShareAsync(fileName, title);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to share inventory backup file {FileName}.", fileName);
            await popup.ShowAlertAsync(popupDefinitions.BackupShareFailed(ex.Message));
        }
    }

    private async Task<FileResult?> PickBackupFileAsync(string title, FilePickerFileType fileType)
        => await filePicker.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = fileType,
        });

    private async Task<FileResult?> PickBackupSigningKeyFileAsync()
        => await PickBackupFileAsync("Choose signing key", JsonBackupFileType);

    private static FilePickerFileType JsonBackupFileType => new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.iOS] = ["public.json"],
        [DevicePlatform.MacCatalyst] = ["public.json"],
        [DevicePlatform.Android] = ["application/json", "text/json", "text/plain"],
        [DevicePlatform.WinUI] = [".json"],
        [DevicePlatform.Tizen] = ["application/json", "text/json", "text/plain"],
    });

    private static FilePickerFileType ZipBackupFileType => new(new Dictionary<DevicePlatform, IEnumerable<string>>
    {
        [DevicePlatform.iOS] = ["public.zip-archive", "com.pkware.zip-archive"],
        [DevicePlatform.MacCatalyst] = ["public.zip-archive", "com.pkware.zip-archive"],
        [DevicePlatform.Android] = ["application/zip", "application/x-zip-compressed"],
        [DevicePlatform.WinUI] = [".zip"],
        [DevicePlatform.Tizen] = ["application/zip", "application/x-zip-compressed"],
    });

    private async Task<string?> SelectBackupFileAsync()
    {
        var fileNames = backupWorkflows.GetBackupFileNames("*.json");

        if (fileNames.Count == 0)
        {
            await popup.ShowAlertAsync(popupDefinitions.NoBackupsFound());
            return null;
        }

        return await popup.SelectOptionAsync(popupDefinitions.BackupFilePicker(fileNames));
    }

    private async Task<string?> SelectZipBackupFileAsync()
    {
        var fileNames = backupWorkflows.GetBackupFileNames("*.zip");

        if (fileNames.Count == 0)
        {
            await popup.ShowAlertAsync(popupDefinitions.NoBackupsFound());
            return null;
        }

        return await popup.SelectOptionAsync(popupDefinitions.BackupFilePicker(fileNames));
    }

    private static string BuildRestoreSummary(
        InventoryBackupRestoreResult result,
        InventoryBackupConflictPolicy policy,
        string fileName,
        int? restoredPhotoFiles = null)
    {
        var photoSummary = restoredPhotoFiles is null
            ? string.Empty
            : $"\nPhoto files restored: {restoredPhotoFiles}";

        return $"Mode: {policy}\nFile: {fileName}\n\n" +
               $"Added: containers {result.AddedContainers}, items {result.AddedItems}, relations {result.AddedRelations}, images {result.AddedImages}\n" +
               $"Updated: containers {result.UpdatedContainers}, items {result.UpdatedItems}\n" +
               $"Deleted: containers {result.DeletedContainers}, items {result.DeletedItems}, relations {result.DeletedRelations}, images {result.DeletedImages}\n" +
               $"Skipped: containers {result.SkippedExistingContainers}, items {result.SkippedExistingItems}, relations {result.SkippedExistingRelations}, images {result.SkippedExistingImages}" +
               photoSummary;
    }

}
