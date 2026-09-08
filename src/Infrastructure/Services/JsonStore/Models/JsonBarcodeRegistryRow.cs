namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonBarcodeRegistryRow
{
    public Guid BarcodeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public int Symbology { get; set; }
    public int Status { get; set; }
    public int? OwnerKind { get; set; }
    public Guid? OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}
