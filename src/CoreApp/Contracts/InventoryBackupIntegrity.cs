namespace CoreApp.Contracts;

public sealed record InventoryBackupIntegrity
{
    public const string Sha256Algorithm = "SHA256";
    public const string HmacSha256SignatureAlgorithm = "HMAC-SHA256";

    public string ChecksumAlgorithm { get; init; } = Sha256Algorithm;
    public string PayloadChecksum { get; init; } = string.Empty;
    public string? SignatureAlgorithm { get; init; }
    public string? Signature { get; init; }
    public string? KeyId { get; init; }
}
