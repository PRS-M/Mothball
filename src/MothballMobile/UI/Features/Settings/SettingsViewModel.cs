using CommunityToolkit.Mvvm.Input;
using CoreApp.Contracts;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private const string BackupsFolder = "Backups";

    private readonly IInventoryBackupExporter backupExporter;
    private readonly IInventoryBackupRestoreService backupRestoreService;
    private readonly IFileHandler fileHandler;
    private readonly IPopupService popup;

    public SettingsViewModel(
        IInventoryBackupExporter backupExporter,
        IInventoryBackupRestoreService backupRestoreService,
        IFileHandler fileHandler,
        IPopupService popup)
    {
        this.backupExporter = backupExporter;
        this.backupRestoreService = backupRestoreService;
        this.fileHandler = fileHandler;
        this.popup = popup;
    }

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

                await popup.ShowAlertAsync(
                    "Backup Exported",
                    $"Backup saved to:\n{fullPath}");
            }
            catch (Exception ex)
            {
                await popup.ShowAlertAsync(
                    "Export Failed",
                    $"Could not export backup to JSON.\n\n{ex.Message}");
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

                await popup.ShowAlertAsync(
                    "Restore Completed",
                    BuildRestoreSummary(result, policy.Value, fileName));
            }
            catch (Exception ex)
            {
                await popup.ShowAlertAsync(
                    "Restore Failed",
                    $"Could not import backup from JSON.\n\n{ex.Message}");
            }
        });
    }

    private static string BuildBackupFileName()
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"mothball-backup-{stamp}Z.json";
    }

    private async Task<InventoryBackupConflictPolicy?> SelectRestorePolicyAsync()
    {
        const string addOnlyLabel = "Add only";
        const string addAndUpsertLabel = "Add + upsert metadata";
        const string fullSyncLabel = "Full sync (roots)";
        const string strictFullSyncLabel = "Strict full sync";

        var selected = await popup.SelectOptionAsync(
            "Restore mode",
            "Cancel",
            addOnlyLabel,
            addAndUpsertLabel,
            fullSyncLabel,
            strictFullSyncLabel);

        return selected switch
        {
            addOnlyLabel => InventoryBackupConflictPolicy.AddOnly,
            addAndUpsertLabel => InventoryBackupConflictPolicy.AddAndUpsertMetadata,
            fullSyncLabel => InventoryBackupConflictPolicy.FullSync,
            strictFullSyncLabel => InventoryBackupConflictPolicy.StrictFullSync,
            _ => null,
        };
    }

    private async Task<string?> SelectBackupFileAsync()
    {
        var fileNames = fileHandler
            .EnumerateFiles(BackupsFolder, "*.json")
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Take(25)
            .ToArray();

        if (fileNames.Length == 0)
        {
            await popup.ShowAlertAsync(
                "No backups found",
                "No JSON backup files were found in local backup storage.");
            return null;
        }

        return await popup.SelectOptionAsync(
            "Choose backup file",
            "Cancel",
            fileNames);
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
