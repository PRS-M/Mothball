namespace MothballMobile.Infrastructure.Backup;

public interface IBackupSignatureSecretProvider
{
    Task<string> GetOrCreateAsync(CancellationToken cancellationToken = default);

    Task ReplaceAsync(string signatureSecret, CancellationToken cancellationToken = default);
}