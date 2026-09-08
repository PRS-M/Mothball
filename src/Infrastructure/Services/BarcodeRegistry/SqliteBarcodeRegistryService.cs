using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.Database;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.BarcodeRegistry;

/// <summary>
/// SQLite implementation of the global barcode registry.
/// </summary>
public sealed class SqliteBarcodeRegistryService : IBarcodeRegistryService
{
    private readonly MothballDatabase database;

    public SqliteBarcodeRegistryService(MothballDatabase database)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<BarcodeRegistryEntry?> FindAsync(string barcodeValue)
    {
        var normalized = Normalize(barcodeValue);
        if (normalized is null) return null;

        await database.InitializeAsync().ConfigureAwait(false);
        var row = await database.Connection.Table<DbBarcodeRegistry>()
            .Where(value => value.NormalizedValue == normalized)
            .FirstOrDefaultAsync().ConfigureAwait(false);
        return row is null ? null : ToDomain(row);
    }

    public async Task<BarcodeRegistryEntry> ReserveAsync(Barcode barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        await database.InitializeAsync().ConfigureAwait(false);
        var existing = await FindAsync(barcode.Value).ConfigureAwait(false);
        if (existing is not null && existing.Status != BarcodeRegistryStatus.Released)
        {
            throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind ?? BarcodeOwnerKind.Item, existing.OwnerName ?? "reserved code");
        }

        var row = new DbBarcodeRegistry
        {
            BarcodeId = existing?.BarcodeId ?? Guid.NewGuid(),
            Value = barcode.Value,
            NormalizedValue = Normalize(barcode.Value)!,
            Symbology = (int)barcode.Symbology,
            Status = (int)BarcodeRegistryStatus.Reserved,
        };
        if (existing is null) await database.Connection.InsertAsync(row).ConfigureAwait(false);
        else await database.Connection.UpdateAsync(row).ConfigureAwait(false);
        return ToDomain(row);
    }

    public async Task AssignAsync(Barcode barcode, BarcodeOwnerKind ownerKind, Guid ownerId, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        await database.InitializeAsync().ConfigureAwait(false);
        var existing = await FindAsync(barcode.Value).ConfigureAwait(false);
        if (existing is not null
            && existing.Status == BarcodeRegistryStatus.Assigned
            && (existing.OwnerKind != ownerKind || existing.OwnerId != ownerId))
        {
            throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind ?? ownerKind, existing.OwnerName ?? ownerName);
        }

        var row = new DbBarcodeRegistry
        {
            BarcodeId = existing?.BarcodeId ?? Guid.NewGuid(),
            Value = barcode.Value,
            NormalizedValue = Normalize(barcode.Value)!,
            Symbology = (int)barcode.Symbology,
            Status = (int)BarcodeRegistryStatus.Assigned,
            OwnerKind = (int)ownerKind,
            OwnerId = ownerId,
            OwnerName = ownerName,
        };
        if (existing is null) await database.Connection.InsertAsync(row).ConfigureAwait(false);
        else await database.Connection.UpdateAsync(row).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string barcodeValue)
    {
        var existing = await FindAsync(barcodeValue).ConfigureAwait(false);
        if (existing is null) return;

        await database.Connection.UpdateAsync(new DbBarcodeRegistry
        {
            BarcodeId = existing.BarcodeId,
            Value = existing.Barcode.Value,
            NormalizedValue = Normalize(existing.Barcode.Value)!,
            Symbology = (int)existing.Barcode.Symbology,
            Status = (int)BarcodeRegistryStatus.Released,
        }).ConfigureAwait(false);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static BarcodeRegistryEntry ToDomain(DbBarcodeRegistry row)
        => new(
            row.BarcodeId,
            new Barcode(row.Value, (BarcodeSymbology)row.Symbology),
            (BarcodeRegistryStatus)row.Status,
            row.OwnerKind is int kind ? (BarcodeOwnerKind)kind : null,
            row.OwnerId,
            string.IsNullOrWhiteSpace(row.OwnerName) ? null : row.OwnerName);
}
