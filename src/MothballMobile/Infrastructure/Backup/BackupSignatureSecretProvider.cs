using System.Security.Cryptography;
using Microsoft.Maui.Storage;

namespace MothballMobile.Infrastructure.Backup;

public sealed class BackupSignatureSecretProvider : IBackupSignatureSecretProvider
{
    public const string SignatureKeyId = "mothball-device-backup-key-v1";

    private const string StorageKey = "mothball.backup.signature-secret.v1";

    private readonly ISecureStorage secureStorage;
    private readonly SemaphoreSlim synchronizationLock = new(1, 1);
    private string? signatureSecret;

    public BackupSignatureSecretProvider(ISecureStorage secureStorage)
    {
        this.secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
    }

    public async Task<string> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(signatureSecret))
        {
            return signatureSecret;
        }

        await synchronizationLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!string.IsNullOrWhiteSpace(signatureSecret))
            {
                return signatureSecret;
            }

            signatureSecret = await secureStorage.GetAsync(StorageKey).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(signatureSecret))
            {
                return signatureSecret;
            }

            signatureSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await secureStorage.SetAsync(StorageKey, signatureSecret).ConfigureAwait(false);
            return signatureSecret;
        }
        finally
        {
            synchronizationLock.Release();
        }
    }
}