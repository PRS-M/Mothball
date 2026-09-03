using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Features.Media;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

/// <summary>JSON implementation for media synchronization metadata.</summary>
public sealed class JsonMediaSyncMetadataRepository(JsonInventoryStore store) : IMediaSyncMetadataRepository
{
    public async Task<MediaSyncMetadata?> GetAsync(Guid workspaceId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var row = (await store.LoadAsync().ConfigureAwait(false)).MediaSyncMetadata.FirstOrDefault(x => x.WorkspaceId == workspaceId && x.ImageId == imageId);
        return row is null ? null : Map(row);
    }

    public Task SaveAsync(MediaSyncMetadata metadata, CancellationToken cancellationToken = default)
        => store.UpdateAsync(state => { state.MediaSyncMetadata = state.MediaSyncMetadata.Where(x => x.WorkspaceId != metadata.WorkspaceId || x.ImageId != metadata.ImageId).Append(ToRow(metadata)).ToList(); return Task.CompletedTask; });

    private static JsonMediaSyncMetadataRow ToRow(MediaSyncMetadata x) => new() { WorkspaceId = x.WorkspaceId, ImageId = x.ImageId, ContentHash = x.ContentHash, MimeType = x.MimeType, ByteLength = x.ByteLength, Width = x.Width, Height = x.Height, RemoteKey = x.RemoteKey, TransferState = (int)x.TransferState, UpdatedUtc = x.UpdatedUtc, Version = x.Version };
    private static MediaSyncMetadata Map(JsonMediaSyncMetadataRow x) => new(x.WorkspaceId, x.ImageId, x.ContentHash, x.MimeType, x.ByteLength, x.Width, x.Height, x.RemoteKey, (MediaTransferState)x.TransferState, x.UpdatedUtc, x.Version);
}
