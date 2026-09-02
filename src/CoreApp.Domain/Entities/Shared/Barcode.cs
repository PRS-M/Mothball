namespace CoreApp.Domain.Entities.Shared;

public sealed record Barcode
{
    public Barcode(string value, BarcodeSymbology symbology)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Barcode value cannot be empty.", nameof(value));
        }

        Value = value.Trim();
        Symbology = symbology;
    }

    public string Value { get; }
    public BarcodeSymbology Symbology { get; }
}

public enum BarcodeSymbology
{
    QrCode,
    Aztec,
    Codabar,
    Code39,
    Code93,
    Code128,
    DataMatrix,
    Ean8,
    Ean13,
    Itf,
    Pdf417,
    UpcA,
    UpcE,
}