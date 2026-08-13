using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using CoreApp.Contracts;

namespace CoreApp.Utilities;

internal static class InventoryBackupPayloadIntegrity
{
    private static readonly JsonSerializerOptions CanonicalDataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static InventoryBackupEnvelope AttachIntegrity(
        InventoryBackupEnvelope backup,
        string? signatureSecret = null,
        string? keyId = null)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);

        string checksum = ComputePayloadChecksum(backup.Data);

        string? signature = null;
        string? signatureAlgorithm = null;
        if (!string.IsNullOrWhiteSpace(signatureSecret))
        {
            signature = ComputeHmacSignature(backup.Data, signatureSecret!);
            signatureAlgorithm = InventoryBackupIntegrity.HmacSha256SignatureAlgorithm;
        }

        return backup with
        {
            Integrity = new InventoryBackupIntegrity
            {
                ChecksumAlgorithm = InventoryBackupIntegrity.Sha256Algorithm,
                PayloadChecksum = checksum,
                SignatureAlgorithm = signatureAlgorithm,
                Signature = signature,
                KeyId = keyId,
            },
        };
    }

    public static void ValidateIntegrity(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        ArgumentNullException.ThrowIfNull(options);

        var integrity = backup.Integrity;
        bool hasChecksum = !string.IsNullOrWhiteSpace(integrity?.PayloadChecksum);

        if (!hasChecksum)
        {
            if (options.RequireIntegrityValidation)
            {
                throw new InvalidDataException("Backup integrity metadata is missing.");
            }

            return;
        }

        if (!string.Equals(integrity!.ChecksumAlgorithm, InventoryBackupIntegrity.Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported checksum algorithm '{integrity.ChecksumAlgorithm}'.");
        }

        string computedChecksum = ComputePayloadChecksum(backup.Data);
        if (!string.Equals(computedChecksum, integrity.PayloadChecksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Backup checksum verification failed.");
        }

        bool hasSignature = !string.IsNullOrWhiteSpace(integrity.Signature);
        bool hasSignatureAlgorithm = !string.IsNullOrWhiteSpace(integrity.SignatureAlgorithm);
        if (hasSignature != hasSignatureAlgorithm)
        {
            throw new InvalidDataException("Backup signature metadata is incomplete.");
        }

        if (!hasSignature)
        {
            return;
        }

        if (!string.Equals(integrity.SignatureAlgorithm, InventoryBackupIntegrity.HmacSha256SignatureAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported signature algorithm '{integrity.SignatureAlgorithm}'.");
        }

        if (string.IsNullOrWhiteSpace(options.SignatureSecret))
        {
            throw new InvalidDataException("Backup includes a signature but no signature secret was provided.");
        }

        string expected = ComputeHmacSignature(backup.Data, options.SignatureSecret);
        if (!FixedTimeEqualsBase64(expected, integrity.Signature!))
        {
            throw new InvalidDataException("Backup signature verification failed.");
        }
    }

    private static string ComputePayloadChecksum(InventoryBackupData data)
    {
        string canonicalData = JsonSerializer.Serialize(data, CanonicalDataJsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(canonicalData);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHmacSignature(InventoryBackupData data, string signatureSecret)
    {
        string canonicalData = JsonSerializer.Serialize(data, CanonicalDataJsonOptions);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(canonicalData);
        byte[] secretBytes = Encoding.UTF8.GetBytes(signatureSecret);

        using var hmac = new HMACSHA256(secretBytes);
        byte[] signatureBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(signatureBytes);
    }

    private static bool FixedTimeEqualsBase64(string expectedBase64, string providedBase64)
    {
        byte[] expected;
        byte[] provided;
        try
        {
            expected = Convert.FromBase64String(expectedBase64);
            provided = Convert.FromBase64String(providedBase64);
        }
        catch (FormatException ex)
        {
            Debug.WriteLine($"Backup signature base64 decoding failed: {ex}");
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
