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
    private bool isZipBackupMode = true;

    public IReadOnlyList<string> ModeOptions { get; } =
    [
        "Auto (System)",
        "Light",
        "Dark",
    ];

    public IReadOnlyList<string> ThemeOptions { get; } =
    [
        "Olive Workshop",
        "Blueprint Ledger",
        "Terracotta Archive",
        "Saffron Utility",
        "Coastal Inventory",
        "Berry Archive",
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

    [RelayCommand]
    private void SelectJsonBackupMode()
        => IsZipBackupMode = false;

    [RelayCommand]
    private void SelectZipBackupMode()
        => IsZipBackupMode = true;

    public string SelectedModeOption
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

    public string SelectedThemeOption
    {
        get => applicationSettings.ThemePalette switch
        {
            ThemePalette.BlueprintLedger => "Blueprint Ledger",
            ThemePalette.TerracottaArchive => "Terracotta Archive",
            ThemePalette.SaffronUtility => "Saffron Utility",
            ThemePalette.CoastalInventory => "Coastal Inventory",
            ThemePalette.BerryArchive => "Berry Archive",
            _ => "Olive Workshop",
        };
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            ThemePalette? palette = value switch
            {
                "Olive Workshop" => ThemePalette.OliveWorkshop,
                "Blueprint Ledger" => ThemePalette.BlueprintLedger,
                "Terracotta Archive" => ThemePalette.TerracottaArchive,
                "Saffron Utility" => ThemePalette.SaffronUtility,
                "Coastal Inventory" => ThemePalette.CoastalInventory,
                "Berry Archive" => ThemePalette.BerryArchive,
                _ => null,
            };

            if (palette is null || applicationSettings.ThemePalette == palette.Value)
            {
                return;
            }

            applicationSettings.ThemePalette = palette.Value;
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

    [RelayCommand]
    private void SelectSimpleAppMode()
        => IsAdvancedAppMode = false;

    [RelayCommand]
    private void SelectAdvancedAppMode()
        => IsAdvancedAppMode = true;

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
        await RunCommandAsync(() => TryWithAlertAsync(
            async () =>
            {
                var export = await backupWorkflows.ExportJsonAsync();
                await popup.ShowAlertAsync(popupDefinitions.BackupExported(export.FullPath));
            },
            "Failed to export inventory backup to JSON.",
            popupDefinitions.BackupExportFailed));
    }

    [RelayCommand]
    private async Task ExportToZipAsync()
    {
        await RunCommandAsync(() => TryWithAlertAsync(
            async () =>
            {
                var export = await backupWorkflows.ExportZipAsync();
                await popup.ShowAlertAsync(popupDefinitions.BackupExported(export.FullPath));
            },
            "Failed to export inventory backup to ZIP.",
            popupDefinitions.BackupExportFailed));
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

            await TryWithAlertAsync(
                () => RestoreFromJsonFileNameAsync(fileName, policy.Value),
                "Failed to import inventory backup from JSON file {FileName}.",
                popupDefinitions.RestoreFailed,
                fileName);
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

            await TryWithAlertAsync(
                async () =>
                {
                    await using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var backupJson = await reader.ReadToEndAsync();

                    await RestoreJsonAsync(backupJson, policy.Value, file.FileName);
                },
                "Failed to import inventory backup from external JSON file {FileName}.",
                popupDefinitions.RestoreFailed,
                file.FileName);
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

            await TryWithAlertAsync(
                async () =>
                {
                    var backupZip = await backupWorkflows.ReadZipAsync(fileName);
                    await RestoreZipAsync(backupZip, policy.Value, fileName);
                },
                "Failed to import inventory backup from ZIP file {FileName}.",
                popupDefinitions.RestoreFailed,
                fileName);
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

            await TryWithAlertAsync(
                async () =>
                {
                    await using var stream = await file.OpenReadAsync();
                    using var memory = new MemoryStream();
                    await stream.CopyToAsync(memory);

                    await RestoreZipAsync(memory.ToArray(), policy.Value, file.FileName);
                },
                "Failed to import inventory backup from external ZIP file {FileName}.",
                popupDefinitions.RestoreFailed,
                file.FileName);
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
        await RunCommandAsync(() => TryWithAlertAsync(
            () => signingKeyTransfer.ShareAsync(),
            "Failed to share the Mothball backup signing key.",
            popupDefinitions.BackupSigningKeyShareFailed));
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

            await TryWithAlertAsync(
                async () =>
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
                },
                "Failed to import the Mothball backup signing key from {FileName}.",
                popupDefinitions.BackupSigningKeyImportFailed,
                file.FileName);
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

            await popup.ConfirmAndRunAsync(popupDefinitions.DeleteBackup(fileName), () => TryWithAlertAsync(
                async () =>
                {
                    await backupWorkflows.DeleteAsync(fileName);
                    await popup.ShowAlertAsync(popupDefinitions.BackupDeleted(fileName));
                },
                "Failed to delete inventory backup JSON file {FileName}.",
                popupDefinitions.DeleteBackupFailed,
                fileName));
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

            await popup.ConfirmAndRunAsync(popupDefinitions.DeleteBackup(fileName), () => TryWithAlertAsync(
                async () =>
                {
                    await backupWorkflows.DeleteAsync(fileName);
                    await popup.ShowAlertAsync(popupDefinitions.BackupDeleted(fileName));
                },
                "Failed to delete inventory backup ZIP file {FileName}.",
                popupDefinitions.DeleteBackupFailed,
                fileName));
        });
    }

    private async Task<InventoryBackupConflictPolicy?> SelectRestorePolicyAsync()
        => await popup.SelectValueOptionAsync(popupDefinitions.RestorePolicyPicker());

    /// <summary>Runs an operation, logging and alerting the given failure popup on exception instead of propagating it.</summary>
    private async Task TryWithAlertAsync(
        Func<Task> action,
        string logMessage,
        Func<string, AlertPopupDefinition> onFailure,
        params object?[] logArgs)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, logMessage, logArgs);
            await popup.ShowAlertAsync(onFailure(ex.Message));
        }
    }

    private async Task RestoreFromJsonFileNameAsync(string fileName, InventoryBackupConflictPolicy policy)
    {
        var backupJson = await backupWorkflows.ReadJsonAsync(fileName);
        await RestoreJsonAsync(backupJson, policy, fileName);
    }

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

    private Task ShareBackupFileAsync(string fileName, string title)
        => TryWithAlertAsync(
            () => backupWorkflows.ShareAsync(fileName, title),
            "Failed to share inventory backup file {FileName}.",
            popupDefinitions.BackupShareFailed,
            fileName);

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
