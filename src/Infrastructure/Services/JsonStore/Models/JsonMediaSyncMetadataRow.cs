namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonMediaSyncMetadataRow
{
    public Guid WorkspaceId { get; set; }
    public Guid ImageId { get; set; }
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
