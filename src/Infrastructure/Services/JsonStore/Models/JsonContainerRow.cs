namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonContainerRow
{
    public int RowId { get; set; }
    public Guid ContainerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string BarcodeValue { get; set; } = string.Empty;
    public int? BarcodeSymbology { get; set; }
}
