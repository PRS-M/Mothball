namespace CoreApp.Application.Features.Media;

/// <summary>State of an image binary relative to its synchronized metadata.</summary>
public enum MediaTransferState { LocalOnly, PendingUpload, AvailableRemotely, PendingDownload, MissingLocal, Deleted }

/// <summary>Synchronizable image metadata; binary content travels outside the change feed.</summary>
public sealed record MediaSyncMetadata(
    Guid WorkspaceId,
    Guid ImageId,
    string ContentHash,
    string MimeType,
    long ByteLength,
    int Width,
    int Height,
    string? RemoteKey,
    MediaTransferState TransferState,
    DateTimeOffset UpdatedUtc,
    long Version = 0);

/// <summary>Port for content-addressed image binary transfer.</summary>
public interface IMediaSyncClient
{
    Task UploadAsync(MediaSyncMetadata metadata, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(MediaSyncMetadata metadata, CancellationToken cancellationToken = default);
}
