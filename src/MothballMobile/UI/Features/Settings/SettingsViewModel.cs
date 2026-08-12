using CommunityToolkit.Mvvm.Input;
using CoreApp.Interfaces;
using MothballMobile.Infrastructure;
using MothballMobile.UI.Shared;

namespace MothballMobile.UI.Features.Settings;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly IInventoryBackupExporter backupExporter;
    private readonly IFileHandler fileHandler;
    private readonly IPopupService popup;

    public SettingsViewModel(
        IInventoryBackupExporter backupExporter,
        IFileHandler fileHandler,
        IPopupService popup)
    {
        this.backupExporter = backupExporter;
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
                var fullPath = await fileHandler.SaveTextFileAsync(fileName, "Backups", backupJson);

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

    private static string BuildBackupFileName()
    {
        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        return $"mothball-backup-{stamp}Z.json";
    }
}
