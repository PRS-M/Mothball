using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace MothballMobile.UI.Features.Settings;

/// <summary>
/// Handles inventory backup export, import, share, and delete for both JSON and ZIP formats.
/// </summary>
public partial class BackupSettingsViewModel : SettingsSectionViewModelBase
{
    private readonly IInventoryBackupWorkflowService backupWorkflows;
    private readonly INavigationService nav;
    private readonly IFilePicker filePicker;

    [ObservableProperty]
    private bool isZipBackupMode = true;

    public BackupSettingsViewModel(
        IInventoryBackupWorkflowService backupWorkflows,
        INavigationService nav,
        IFilePicker filePicker,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ILogger<BackupSettingsViewModel> logger)
        : base(popup, popupDefinitions, logger)
    {
        this.backupWorkflows = backupWorkflows ?? throw new ArgumentNullException(nameof(backupWorkflows));
        this.nav = nav;
        this.filePicker = filePicker;
    }

    public bool IsJsonBackupMode => !IsZipBackupMode;

    partial void OnIsZipBackupModeChanged(bool value)
        => OnPropertyChanged(nameof(IsJsonBackupMode));

    [RelayCommand]
    private void SelectJsonBackupMode()
        => IsZipBackupMode = false;

    [RelayCommand]
    private void SelectZipBackupMode()
        => IsZipBackupMode = true;

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
                await Popup.ShowAlertAsync(PopupDefinitions.BackupExported(export.FullPath));
            },
            "Failed to export inventory backup to JSON.",
            PopupDefinitions.BackupExportFailed));
    }

    [RelayCommand]
    private async Task ExportToZipAsync()
    {
        await RunCommandAsync(() => TryWithAlertAsync(
            async () =>
            {
                var export = await backupWorkflows.ExportZipAsync();
                await Popup.ShowAlertAsync(PopupDefinitions.BackupExported(export.FullPath));
            },
            "Failed to export inventory backup to ZIP.",
            PopupDefinitions.BackupExportFailed));
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
                PopupDefinitions.RestoreFailed,
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
                PopupDefinitions.RestoreFailed,
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
                PopupDefinitions.RestoreFailed,
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
                PopupDefinitions.RestoreFailed,
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
    private async Task DeleteJsonAsync()
    {
        await RunCommandAsync(async () =>
        {
            var fileName = await SelectBackupFileAsync();
            if (string.IsNullOrWhiteSpace(fileName))
                return;

            await Popup.ConfirmAndRunAsync(PopupDefinitions.DeleteBackup(fileName), () => TryWithAlertAsync(
                async () =>
                {
                    await backupWorkflows.DeleteAsync(fileName);
                    await Popup.ShowAlertAsync(PopupDefinitions.BackupDeleted(fileName));
                },
                "Failed to delete inventory backup JSON file {FileName}.",
                PopupDefinitions.DeleteBackupFailed,
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

            await Popup.ConfirmAndRunAsync(PopupDefinitions.DeleteBackup(fileName), () => TryWithAlertAsync(
                async () =>
                {
                    await backupWorkflows.DeleteAsync(fileName);
                    await Popup.ShowAlertAsync(PopupDefinitions.BackupDeleted(fileName));
                },
                "Failed to delete inventory backup ZIP file {FileName}.",
                PopupDefinitions.DeleteBackupFailed,
                fileName));
        });
    }

    private async Task<InventoryBackupConflictPolicy?> SelectRestorePolicyAsync()
        => await Popup.SelectValueOptionAsync(PopupDefinitions.RestorePolicyPicker());

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

        await Popup.ShowAlertAsync(PopupDefinitions.RestoreCompleted(BuildRestoreSummary(result, policy, fileName)));
    }

    private async Task RestoreZipAsync(
        byte[] backupZip,
        InventoryBackupConflictPolicy policy,
        string fileName)
    {
        var restore = await backupWorkflows.RestoreZipAsync(backupZip, policy);

        await Popup.ShowAlertAsync(PopupDefinitions.RestoreCompleted(BuildRestoreSummary(restore.Result, policy, fileName, restore.RestoredPhotoFiles)));
    }

    private Task ShareBackupFileAsync(string fileName, string title)
        => TryWithAlertAsync(
            () => backupWorkflows.ShareAsync(fileName, title),
            "Failed to share inventory backup file {FileName}.",
            PopupDefinitions.BackupShareFailed,
            fileName);

    private async Task<FileResult?> PickBackupFileAsync(string title, FilePickerFileType fileType)
        => await filePicker.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = fileType,
        });

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
            await Popup.ShowAlertAsync(PopupDefinitions.NoBackupsFound());
            return null;
        }

        return await Popup.SelectOptionAsync(PopupDefinitions.BackupFilePicker(fileNames));
    }

    private async Task<string?> SelectZipBackupFileAsync()
    {
        var fileNames = backupWorkflows.GetBackupFileNames("*.zip");

        if (fileNames.Count == 0)
        {
            await Popup.ShowAlertAsync(PopupDefinitions.NoBackupsFound());
            return null;
        }

        return await Popup.SelectOptionAsync(PopupDefinitions.BackupFilePicker(fileNames));
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
