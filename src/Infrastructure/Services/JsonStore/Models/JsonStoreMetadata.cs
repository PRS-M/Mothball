namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonStoreMetadata
{
    public int SchemaVersion { get; set; } = 2;

    public int NextContainerRowId { get; set; } = 1;
    public int NextItemRowId { get; set; } = 1;
    public int NextImageRowId { get; set; } = 1;
    public int NextRelationId { get; set; } = 1;
}
