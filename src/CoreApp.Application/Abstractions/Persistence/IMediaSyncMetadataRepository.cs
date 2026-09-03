using CoreApp.Application.Features.Media;

namespace CoreApp.Application.Abstractions.Persistence;

/// <summary>Persists synchronizable image metadata without embedding binary content in inventory operations.</summary>
public interface IMediaSyncMetadataRepository
{
    Task<MediaSyncMetadata?> GetAsync(Guid workspaceId, Guid imageId, CancellationToken cancellationToken = default);
    Task SaveAsync(MediaSyncMetadata metadata, CancellationToken cancellationToken = default);
}
