using CoreApp.Application.Features.Media;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Repositories;

/// <summary>SQLite implementation for media synchronization metadata.</summary>
public sealed class SqliteMediaSyncMetadataRepository(MothballDatabase database) : IMediaSyncMetadataRepository
{
    public async Task<MediaSyncMetadata?> GetAsync(Guid workspaceId, Guid imageId, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        var row = await database.Connection.FindAsync<DbMediaSyncMetadata>(Id(workspaceId, imageId)).ConfigureAwait(false);
        return row is null ? null : Map(row);
    }

    public async Task SaveAsync(MediaSyncMetadata metadata, CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync().ConfigureAwait(false);
        await database.Connection.InsertOrReplaceAsync(ToRow(metadata)).ConfigureAwait(false);
    }

    private static string Id(Guid workspaceId, Guid imageId) => $"{workspaceId:N}:{imageId:N}";
    private static DbMediaSyncMetadata ToRow(MediaSyncMetadata x) => new() { MetadataId = Id(x.WorkspaceId, x.ImageId), WorkspaceId = x.WorkspaceId, ImageId = x.ImageId, ContentHash = x.ContentHash, MimeType = x.MimeType, ByteLength = x.ByteLength, Width = x.Width, Height = x.Height, RemoteKey = x.RemoteKey, TransferState = (int)x.TransferState, UpdatedUtc = x.UpdatedUtc, Version = x.Version };
    private static MediaSyncMetadata Map(DbMediaSyncMetadata x) => new(x.WorkspaceId, x.ImageId, x.ContentHash, x.MimeType, x.ByteLength, x.Width, x.Height, x.RemoteKey, (MediaTransferState)x.TransferState, x.UpdatedUtc, x.Version);
}
