namespace MothballMobile.Infrastructure.Backup;

public interface IBackupSigningKeyTransferService
{
    Task ShareAsync(CancellationToken cancellationToken = default);

    Task ImportAsync(Stream transferStream, CancellationToken cancellationToken = default);
}