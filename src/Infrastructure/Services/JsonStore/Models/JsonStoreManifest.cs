namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonStoreManifest
{
    public int Generation { get; set; }

    // "A" or "B"
    public string CurrentSlot { get; set; } = "A";
    public string PreviousSlot { get; set; } = "A";

    public int SchemaVersion { get; set; } = 1;
}
