using CommunityToolkit.Mvvm.Input;
using CoreApp.Contracts;
using CoreApp.Interfaces;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Popups;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private const string BackupsFolder = "Backups";

    private readonly IInventoryBackupExporter backupExporter;
    private readonly IInventoryBackupRestoreService backupRestoreService;
    private readonly IFileHandler fileHandler;
    private readonly INavigationService nav;
    private readonly IPopupService popup;
    private readonly IPopupDefinitionService popupDefinitions;
    private readonly ILogger<SettingsViewModel> logger;

    public SettingsViewModel(
        IInventoryBackupExporter backupExporter,
        IInventoryBackupRestoreService backupRestoreService,
        IFileHandler fileHandler,
        INavigationService nav,
        IPopupService popup,
        IPopupDefinitionService popupDefinitions,
        ILogger<SettingsViewModel> logger)
    {
        this.backupExporter = backupExporter;
        this.backupRestoreService = backupRestoreService;
        this.fileHandler = fileHandler;
        this.nav = nav;
        this.popup = popup;
        this.popupDefinitions = popupDefinitions;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                var backupJson = await backupExporter.ExportAsJsonAsync();
                var fileName = BuildBackupFileName();
                var fullPath = await fileHandler.SaveTextFileAsync(fileName, BackupsFolder, backupJson);

                await popup.ShowAlertAsync(popupDefinitions.BackupExported(fullPath));
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
                var backupZip = await backupExporter.ExportAsZipAsync();
                var fileName = BuildBackupZipFileName();
                var fullPath = await fileHandler.SaveFileAsync(fileName, BackupsFolder, backupZip);

                await popup.ShowAlertAsync(popupDefinitions.BackupExported(fullPath));
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
                var backupJson = await fileHandler.ReadTextFileAsync(fileName, BackupsFolder);
                var options = new InventoryBackupRestoreOptions
                {
                    ConflictPolicy = policy.Value,
                };

                var result = await backupRestoreService.RestoreFromJsonAsync(backupJson, options);

                await popup.ShowAlertAsync(popupDefinitions.RestoreCompleted(BuildRestoreSummary(result, policy.Value, fileName)));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to import inventory backup from JSON file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.RestoreFailed(ex.Message));
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
                await fileHandler.DeleteFileAsync(fileName, BackupsFolder);
                await popup.ShowAlertAsync(popupDefinitions.BackupDeleted(fileName));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete inventory backup JSON file {FileName}.", fileName);
                await popup.ShowAlertAsync(popupDefinitions.DeleteBackupFailed(ex.Message));
            }
        });
    }

    private static string BuildBackupFileName()
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"mothball-backup-{stamp}Z.json";
    }

    private static string BuildBackupZipFileName()
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"mothball-backup-{stamp}Z.zip";
    }

    private async Task<InventoryBackupConflictPolicy?> SelectRestorePolicyAsync()
        => await popup.SelectValueOptionAsync(popupDefinitions.RestorePolicyPicker());

    private async Task<string?> SelectBackupFileAsync()
    {
        var fileNames = fileHandler
            .EnumerateFiles(BackupsFolder, "*.json")
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Take(25)
            .ToArray();

        if (fileNames.Length == 0)
        {
            await popup.ShowAlertAsync(popupDefinitions.NoBackupsFound());
            return null;
        }

        return await popup.SelectOptionAsync(popupDefinitions.BackupFilePicker(fileNames));
    }

    private static string BuildRestoreSummary(
        InventoryBackupRestoreResult result,
        InventoryBackupConflictPolicy policy,
        string fileName)
    {
        return $"Mode: {policy}\nFile: {fileName}\n\n" +
               $"Added: containers {result.AddedContainers}, items {result.AddedItems}, relations {result.AddedRelations}, images {result.AddedImages}\n" +
               $"Updated: containers {result.UpdatedContainers}, items {result.UpdatedItems}\n" +
               $"Deleted: containers {result.DeletedContainers}, items {result.DeletedItems}, relations {result.DeletedRelations}, images {result.DeletedImages}\n" +
               $"Skipped: containers {result.SkippedExistingContainers}, items {result.SkippedExistingItems}, relations {result.SkippedExistingRelations}, images {result.SkippedExistingImages}";
    }
}
