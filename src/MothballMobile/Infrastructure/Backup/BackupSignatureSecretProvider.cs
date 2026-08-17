using System.Security.Cryptography;
using Microsoft.Maui.Storage;
#if IOS || MACCATALYST
using Foundation;
using Security;
#endif

namespace MothballMobile.Infrastructure.Backup;

public sealed class BackupSignatureSecretProvider : IBackupSignatureSecretProvider
{
    public const string SignatureKeyId = "mothball-device-backup-key-v1";

    private const string StorageKey = "mothball.backup.signature-secret.v1";
    private const string KeychainService = "com.prsmcodeit.mothball.backup";
    private const string KeychainAccount = "signature-secret.v1";

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

            signatureSecret = GetSynchronizedSecret();
            if (string.IsNullOrWhiteSpace(signatureSecret))
            {
                signatureSecret = await secureStorage.GetAsync(StorageKey).ConfigureAwait(false)
                    ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
                signatureSecret = StoreSynchronizedSecret(signatureSecret);
            }

            await secureStorage.SetAsync(StorageKey, signatureSecret).ConfigureAwait(false);
            return signatureSecret;
        }
        finally
        {
            synchronizationLock.Release();
        }
    }

    private static string? GetSynchronizedSecret()
    {
#if IOS || MACCATALYST
        using var query = CreateKeychainRecord();
        using var record = SecKeyChain.QueryAsRecord(query, out var status);
        if (status == SecStatusCode.Success && record?.ValueData is not null)
        {
            return NSString.FromData(record.ValueData, NSStringEncoding.UTF8);
        }

        if (status != SecStatusCode.ItemNotFound)
        {
            throw new InvalidOperationException($"Could not read the backup signing key from the iCloud Keychain: {status}.");
        }
#endif

        return null;
    }

    private static string StoreSynchronizedSecret(string secret)
    {
#if IOS || MACCATALYST
        using var record = CreateKeychainRecord();
        record.ValueData = NSData.FromString(secret, NSStringEncoding.UTF8);

        var status = SecKeyChain.Add(record);
        if (status == SecStatusCode.Success)
        {
            return secret;
        }

        if (status == SecStatusCode.DuplicateItem)
        {
            return GetSynchronizedSecret()
                ?? throw new InvalidOperationException("The iCloud Keychain backup signing key could not be retrieved after a concurrent write.");
        }

        throw new InvalidOperationException($"Could not save the backup signing key to the iCloud Keychain: {status}.");
#else
        return secret;
#endif
    }

#if IOS || MACCATALYST
    private static SecRecord CreateKeychainRecord()
        => new(SecKind.GenericPassword)
        {
            Service = KeychainService,
            Account = KeychainAccount,
            Synchronizable = true,
            Accessible = SecAccessible.AfterFirstUnlock,
        };
#endif
}