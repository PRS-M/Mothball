using CoreApp.Contracts;

namespace MothballMobile.Infrastructure.Backup;

public interface IInventoryBackupWorkflowService
{
    Task<InventoryBackupExportResult> ExportJsonAsync(CancellationToken cancellationToken = default);

    Task<InventoryBackupExportResult> ExportZipAsync(CancellationToken cancellationToken = default);

    Task<string> ReadJsonAsync(string fileName, CancellationToken cancellationToken = default);

    Task<byte[]> ReadZipAsync(string fileName, CancellationToken cancellationToken = default);

    Task<InventoryBackupRestoreResult> RestoreJsonAsync(
        string backupJson,
        InventoryBackupConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default);

    Task<InventoryBackupZipRestoreResult> RestoreZipAsync(
        byte[] backupZip,
        InventoryBackupConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetBackupFileNames(string searchPattern);

    Task ShareAsync(string fileName, string title);

    Task DeleteAsync(string fileName);
}

public sealed record InventoryBackupExportResult(string FileName, string FullPath);