namespace CoreApp.Application.Contracts;

public sealed record BarcodeLookupResult(
    BarcodeOwnerKind OwnerKind,
    Guid OwnerId,
    string OwnerName);

public enum BarcodeOwnerKind
{
    Container,
    Item,
}