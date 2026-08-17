using CoreApp.Contracts;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace MothballMobile.Infrastructure.Backup;

public sealed class InventoryBackupWorkflowService : IInventoryBackupWorkflowService
{
    private const string BackupsFolder = "Backups";

    private readonly IInventoryBackupExporter backupExporter;
    private readonly IInventoryBackupRestoreService backupRestoreService;
    private readonly IInventoryBackupZipRestoreService backupZipRestoreService;
    private readonly IBackupSignatureSecretProvider signatureSecretProvider;
    private readonly IFileHandler fileHandler;
    private readonly IShare share;

    public InventoryBackupWorkflowService(
        IInventoryBackupExporter backupExporter,
        IInventoryBackupRestoreService backupRestoreService,
        IInventoryBackupZipRestoreService backupZipRestoreService,
        IBackupSignatureSecretProvider signatureSecretProvider,
        IFileHandler fileHandler,
        IShare share)
    {
        this.backupExporter = backupExporter ?? throw new ArgumentNullException(nameof(backupExporter));
        this.backupRestoreService = backupRestoreService ?? throw new ArgumentNullException(nameof(backupRestoreService));
        this.backupZipRestoreService = backupZipRestoreService ?? throw new ArgumentNullException(nameof(backupZipRestoreService));
        this.signatureSecretProvider = signatureSecretProvider ?? throw new ArgumentNullException(nameof(signatureSecretProvider));
        this.fileHandler = fileHandler ?? throw new ArgumentNullException(nameof(fileHandler));
        this.share = share ?? throw new ArgumentNullException(nameof(share));
    }

    public async Task<InventoryBackupExportResult> ExportJsonAsync(CancellationToken cancellationToken = default)
    {
        var signatureSecret = await signatureSecretProvider.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        var backupJson = await backupExporter.ExportAsJsonAsync(
            signatureSecret,
            BackupSignatureSecretProvider.SignatureKeyId,
            cancellationToken).ConfigureAwait(false);
        var fileName = BuildBackupFileName("json");
        var fullPath = await fileHandler.SaveTextFileAsync(fileName, BackupsFolder, backupJson).ConfigureAwait(false);
        return new InventoryBackupExportResult(fileName, fullPath);
    }

    public async Task<InventoryBackupExportResult> ExportZipAsync(CancellationToken cancellationToken = default)
    {
        var signatureSecret = await signatureSecretProvider.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        var backupZip = await backupExporter.ExportAsZipAsync(
            signatureSecret,
            BackupSignatureSecretProvider.SignatureKeyId,
            cancellationToken).ConfigureAwait(false);
        var fileName = BuildBackupFileName("zip");
        var fullPath = await fileHandler.SaveFileAsync(fileName, BackupsFolder, backupZip).ConfigureAwait(false);
        return new InventoryBackupExportResult(fileName, fullPath);
    }

    public Task<string> ReadJsonAsync(string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();
        return fileHandler.ReadTextFileAsync(fileName, BackupsFolder);
    }

    public Task<byte[]> ReadZipAsync(string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        cancellationToken.ThrowIfCancellationRequested();
        return fileHandler.ReadFileAsync(fileName, BackupsFolder);
    }

    public async Task<InventoryBackupRestoreResult> RestoreJsonAsync(
        string backupJson,
        InventoryBackupConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default)
    {
        var options = await CreateRestoreOptionsAsync(conflictPolicy, cancellationToken).ConfigureAwait(false);
        return await backupRestoreService.RestoreFromJsonAsync(backupJson, options, cancellationToken).ConfigureAwait(false);
    }

    public async Task<InventoryBackupZipRestoreResult> RestoreZipAsync(
        byte[] backupZip,
        InventoryBackupConflictPolicy conflictPolicy,
        CancellationToken cancellationToken = default)
    {
        var options = await CreateRestoreOptionsAsync(conflictPolicy, cancellationToken).ConfigureAwait(false);
        return await backupZipRestoreService.RestoreFromZipAsync(backupZip, options, cancellationToken).ConfigureAwait(false);
    }

    public IReadOnlyList<string> GetBackupFileNames(string searchPattern)
        => fileHandler
            .EnumerateFiles(BackupsFolder, searchPattern)
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Take(25)
            .ToArray();

    public Task ShareAsync(string fileName, string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var fullPath = Path.Combine(fileHandler.AppDataPath, BackupsFolder, fileName);
        return share.RequestAsync(new ShareFileRequest(title, new ShareFile(fullPath)));
    }

    public Task DeleteAsync(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return fileHandler.DeleteFileAsync(fileName, BackupsFolder);
    }

    private async Task<InventoryBackupRestoreOptions> CreateRestoreOptionsAsync(
        InventoryBackupConflictPolicy conflictPolicy,
        CancellationToken cancellationToken)
        => new()
        {
            ConflictPolicy = conflictPolicy,
            SignatureSecret = await signatureSecretProvider.GetOrCreateAsync(cancellationToken).ConfigureAwait(false),
        };

    private static string BuildBackupFileName(string extension)
        => $"mothball-backup-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}Z.{extension}";
}