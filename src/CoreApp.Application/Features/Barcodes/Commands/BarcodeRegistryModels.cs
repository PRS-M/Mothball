using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Contracts;

namespace CoreApp.Application.Features.Barcodes.Commands;

/// <summary>
/// Describes the lifecycle of a generated or assigned barcode.
/// </summary>
public enum BarcodeRegistryStatus
{
    Reserved,
    Assigned,
    Released,
}

/// <summary>
/// Represents a barcode tracked independently from its inventory owner.
/// </summary>
public sealed record BarcodeRegistryEntry(
    Guid BarcodeId,
    Barcode Barcode,
    BarcodeRegistryStatus Status,
    BarcodeOwnerKind? OwnerKind = null,
    Guid? OwnerId = null,
    string? OwnerName = null);

/// <summary>
/// Persists globally unique barcode reservations and assignments.
/// </summary>
public interface IBarcodeRegistryService
{
    /// <summary>
    /// Finds a barcode by its trimmed, case-sensitive value.
    /// </summary>
    Task<BarcodeRegistryEntry?> FindAsync(string barcodeValue);

    /// <summary>
    /// Reserves an unassigned barcode for later claiming or printing.
    /// </summary>
    Task<BarcodeRegistryEntry> ReserveAsync(Barcode barcode);

    /// <summary>
    /// Assigns a barcode to an inventory record, or claims its existing reservation.
    /// </summary>
    Task AssignAsync(Barcode barcode, BarcodeOwnerKind ownerKind, Guid ownerId, string ownerName);

    /// <summary>
    /// Releases a reservation so it can no longer be claimed.
    /// </summary>
    Task ReleaseAsync(string barcodeValue);
}
