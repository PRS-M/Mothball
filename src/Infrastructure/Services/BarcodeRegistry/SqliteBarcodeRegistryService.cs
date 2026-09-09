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
    private const int MaximumBatchSize = 1000;
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

    public async Task<IReadOnlyList<BarcodeRegistryEntry>> ReserveInternalSkuBatchAsync(int count)
    {
        if (count is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The SKU batch size must be between 1 and 1000.");
        }

        await database.InitializeAsync().ConfigureAwait(false);
        var result = new List<BarcodeRegistryEntry>(count);
        await database.Connection.RunInTransactionAsync(connection =>
        {
            for (var index = 0; index < count; index++)
            {
                var barcode = BarcodeGenerator.CreateInternalSku(Guid.NewGuid());
                var row = new DbBarcodeRegistry
                {
                    BarcodeId = Guid.NewGuid(),
                    Value = barcode.Value,
                    NormalizedValue = barcode.Value,
                    Symbology = (int)barcode.Symbology,
                    Status = (int)BarcodeRegistryStatus.Reserved,
                };
                connection.Insert(row);
                result.Add(ToDomain(row));
            }
        }).ConfigureAwait(false);
        return result;
    }

    public async Task<BarcodeRegistryEntry> ReserveAsync(Barcode barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        await database.InitializeAsync().ConfigureAwait(false);
        DbBarcodeRegistry? reserved = null;
        await database.RunInTransactionAsync(connection =>
        {
            var normalized = Normalize(barcode.Value)!;
            var existing = connection.Table<DbBarcodeRegistry>()
                .FirstOrDefault(value => value.NormalizedValue == normalized);
            if (existing is not null)
            {
                throw new BarcodeAlreadyAssignedException(
                    barcode.Value,
                    existing.OwnerKind is int kind ? (BarcodeOwnerKind)kind : BarcodeOwnerKind.Item,
                    string.IsNullOrWhiteSpace(existing.OwnerName) ? "reserved code" : existing.OwnerName);
            }

            reserved = new DbBarcodeRegistry
            {
                Value = barcode.Value,
                NormalizedValue = normalized,
                Symbology = (int)barcode.Symbology,
                Status = (int)BarcodeRegistryStatus.Reserved,
            };
            connection.Insert(reserved);
        }).ConfigureAwait(false);
        return ToDomain(reserved!);
    }

    public async Task AssignAsync(Barcode barcode, BarcodeOwnerKind ownerKind, Guid ownerId, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            var normalized = Normalize(barcode.Value)!;
            var existing = connection.Table<DbBarcodeRegistry>()
                .FirstOrDefault(value => value.NormalizedValue == normalized);
            if (existing?.Status == (int)BarcodeRegistryStatus.Released)
            {
                throw new BarcodeAlreadyAssignedException(
                    barcode.Value,
                    existing.OwnerKind is int kind ? (BarcodeOwnerKind)kind : BarcodeOwnerKind.Item,
                    string.IsNullOrWhiteSpace(existing.OwnerName) ? "released code" : existing.OwnerName);
            }

            if (existing is not null
                && existing.Status == (int)BarcodeRegistryStatus.Assigned
                && (existing.OwnerKind != (int)ownerKind || existing.OwnerId != ownerId))
            {
                throw new BarcodeAlreadyAssignedException(
                    barcode.Value,
                    existing.OwnerKind is int kind ? (BarcodeOwnerKind)kind : ownerKind,
                    string.IsNullOrWhiteSpace(existing.OwnerName) ? ownerName : existing.OwnerName);
            }

            var row = existing ?? new DbBarcodeRegistry
            {
                BarcodeId = Guid.NewGuid(),
                NormalizedValue = normalized,
            };
            row.Value = barcode.Value;
            row.Symbology = (int)barcode.Symbology;
            row.Status = (int)BarcodeRegistryStatus.Assigned;
            row.OwnerKind = (int)ownerKind;
            row.OwnerId = ownerId;
            row.OwnerName = ownerName;
            if (existing is null) connection.Insert(row);
            else connection.Update(row);
        }).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string barcodeValue)
    {
        var normalized = Normalize(barcodeValue);
        if (normalized is null) return;

        await database.InitializeAsync().ConfigureAwait(false);
        await database.RunInTransactionAsync(connection =>
        {
            var existing = connection.Table<DbBarcodeRegistry>()
                .FirstOrDefault(value => value.NormalizedValue == normalized);
            if (existing is null) return;

            existing.Status = (int)BarcodeRegistryStatus.Released;
            existing.OwnerKind = null;
            existing.OwnerId = null;
            existing.OwnerName = string.Empty;
            connection.Update(existing);
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
