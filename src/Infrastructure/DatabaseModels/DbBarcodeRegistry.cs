using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public sealed class DbBarcodeRegistry
{
    [PrimaryKey, NotNull]
    public Guid BarcodeId { get; set; } = Guid.NewGuid();

    [NotNull, Indexed("UX_DbBarcodeRegistry_NormalizedValue", 1, Unique = true)]
    public string NormalizedValue { get; set; } = string.Empty;

    [NotNull]
    public string Value { get; set; } = string.Empty;

    [NotNull]
    public int Symbology { get; set; }

    [NotNull]
    public int Status { get; set; }

    public int? OwnerKind { get; set; }
    public Guid? OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
}
