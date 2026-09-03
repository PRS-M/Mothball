using SQLite;

namespace Infrastructure.Services.DatabaseModels;

[Table("MediaSyncMetadata")]
public sealed class DbMediaSyncMetadata
{
    [PrimaryKey, NotNull] public string MetadataId { get; set; } = string.Empty;
    [Indexed, NotNull] public Guid WorkspaceId { get; set; }
    [Indexed, NotNull] public Guid ImageId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long ByteLength { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string? RemoteKey { get; set; }
    public int TransferState { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public long Version { get; set; }
}
