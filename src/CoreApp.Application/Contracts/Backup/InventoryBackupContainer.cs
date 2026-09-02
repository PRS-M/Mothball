namespace CoreApp.Application.Contracts.Backup;

public sealed record InventoryBackupContainer
{
    public Guid ContainerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public string BarcodeValue { get; init; } = string.Empty;
    public int? BarcodeSymbology { get; init; }
}