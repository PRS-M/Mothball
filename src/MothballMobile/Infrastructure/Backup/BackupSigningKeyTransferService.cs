using System.Text.Json;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;

namespace MothballMobile.Infrastructure.Backup;

public sealed class BackupSigningKeyTransferService : IBackupSigningKeyTransferService
{
    private const string TransferFileName = "mothball-backup-signing-key.json";
    private const int TransferFormatVersion = 1;

    private readonly IBackupSignatureSecretProvider signatureSecretProvider;
    private readonly IShare share;

    public BackupSigningKeyTransferService(
        IBackupSignatureSecretProvider signatureSecretProvider,
        IShare share)
    {
        this.signatureSecretProvider = signatureSecretProvider ?? throw new ArgumentNullException(nameof(signatureSecretProvider));
        this.share = share ?? throw new ArgumentNullException(nameof(share));
    }

    public async Task ShareAsync(CancellationToken cancellationToken = default)
    {
        var signatureSecret = await signatureSecretProvider.GetOrCreateAsync(cancellationToken).ConfigureAwait(false);
        var transfer = new BackupSigningKeyTransfer(
            TransferFormatVersion,
            BackupSignatureSecretProvider.SignatureKeyId,
            signatureSecret);
        var transferPath = Path.Combine(FileSystem.CacheDirectory, TransferFileName);
        await File.WriteAllTextAsync(transferPath, JsonSerializer.Serialize(transfer), cancellationToken).ConfigureAwait(false);
        await share.RequestAsync(new ShareFileRequest("Share Mothball backup signing key", new ShareFile(transferPath))).ConfigureAwait(false);
    }

    public async Task ImportAsync(Stream transferStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transferStream);

        var transfer = await JsonSerializer.DeserializeAsync<BackupSigningKeyTransfer>(transferStream, cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidDataException("The signing key file is empty or invalid.");

        if (transfer.FormatVersion != TransferFormatVersion ||
            !string.Equals(transfer.KeyId, BackupSignatureSecretProvider.SignatureKeyId, StringComparison.Ordinal))
        {
            throw new InvalidDataException("The signing key file is not compatible with this version of Mothball.");
        }

        await signatureSecretProvider.ReplaceAsync(transfer.SignatureSecret, cancellationToken).ConfigureAwait(false);
    }

    private sealed record BackupSigningKeyTransfer(
        int FormatVersion,
        string KeyId,
        string SignatureSecret);
}