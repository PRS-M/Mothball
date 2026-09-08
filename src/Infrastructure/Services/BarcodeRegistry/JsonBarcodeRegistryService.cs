using CoreApp.Application.Contracts;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.BarcodeRegistry;

/// <summary>
/// JSON-store implementation of the global barcode registry.
/// </summary>
public sealed class JsonBarcodeRegistryService : IBarcodeRegistryService
{
    private const int MaximumBatchSize = 1000;
    private readonly JsonInventoryStore store;

    public JsonBarcodeRegistryService(JsonInventoryStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<BarcodeRegistryEntry?> FindAsync(string barcodeValue)
    {
        var normalized = Normalize(barcodeValue);
        if (normalized is null) return null;
        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Barcodes.FirstOrDefault(value => value.NormalizedValue == normalized);
        return row is null ? null : ToDomain(row);
    }

    public async Task<IReadOnlyList<BarcodeRegistryEntry>> ReserveInternalSkuBatchAsync(int count)
    {
        if (count is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "The SKU batch size must be between 1 and 1000.");
        }

        var result = new List<BarcodeRegistryEntry>(count);
        await store.UpdateAsync(state =>
        {
            for (var index = 0; index < count; index++)
            {
                var barcode = InternalSkuGenerator.Create(Guid.NewGuid());
                var row = new JsonBarcodeRegistryRow
                {
                    BarcodeId = Guid.NewGuid(),
                    Value = barcode.Value,
                    NormalizedValue = barcode.Value,
                    Symbology = (int)barcode.Symbology,
                    Status = (int)BarcodeRegistryStatus.Reserved,
                };
                state.Barcodes.Add(row);
                result.Add(ToDomain(row));
            }
            return Task.CompletedTask;
        }).ConfigureAwait(false);
        return result;
    }

    public async Task<BarcodeRegistryEntry> ReserveAsync(Barcode barcode)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        var normalized = Normalize(barcode.Value)!;
        BarcodeRegistryEntry? result = null;
        await store.UpdateAsync(state =>
        {
            var existing = state.Barcodes.FirstOrDefault(value => value.NormalizedValue == normalized);
            if (existing is not null)
            {
                throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind is int kind ? (BarcodeOwnerKind)kind : BarcodeOwnerKind.Item, existing.OwnerName);
            }

            var row = existing ?? new JsonBarcodeRegistryRow { BarcodeId = Guid.NewGuid() };
            row.Value = barcode.Value;
            row.NormalizedValue = normalized;
            row.Symbology = (int)barcode.Symbology;
            row.Status = (int)BarcodeRegistryStatus.Reserved;
            row.OwnerKind = null;
            row.OwnerId = null;
            row.OwnerName = string.Empty;
            if (existing is null) state.Barcodes.Add(row);
            result = ToDomain(row);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
        return result!;
    }

    public async Task AssignAsync(Barcode barcode, BarcodeOwnerKind ownerKind, Guid ownerId, string ownerName)
    {
        ArgumentNullException.ThrowIfNull(barcode);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerName);
        var normalized = Normalize(barcode.Value)!;
        await store.UpdateAsync(state =>
        {
            var existing = state.Barcodes.FirstOrDefault(value => value.NormalizedValue == normalized);
            if (existing?.Status == (int)BarcodeRegistryStatus.Released)
            {
                throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind is int releasedKind ? (BarcodeOwnerKind)releasedKind : BarcodeOwnerKind.Item, existing.OwnerName);
            }

            if (existing is not null
                && existing.Status == (int)BarcodeRegistryStatus.Assigned
                && (existing.OwnerKind != (int)ownerKind || existing.OwnerId != ownerId))
            {
                throw new BarcodeAlreadyAssignedException(barcode.Value, existing.OwnerKind is int kind ? (BarcodeOwnerKind)kind : ownerKind, existing.OwnerName);
            }

            var row = existing ?? new JsonBarcodeRegistryRow { BarcodeId = Guid.NewGuid() };
            row.Value = barcode.Value;
            row.NormalizedValue = normalized;
            row.Symbology = (int)barcode.Symbology;
            row.Status = (int)BarcodeRegistryStatus.Assigned;
            row.OwnerKind = (int)ownerKind;
            row.OwnerId = ownerId;
            row.OwnerName = ownerName;
            if (existing is null) state.Barcodes.Add(row);
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    public async Task ReleaseAsync(string barcodeValue)
    {
        var normalized = Normalize(barcodeValue);
        if (normalized is null) return;
        await store.UpdateAsync(state =>
        {
            var row = state.Barcodes.FirstOrDefault(value => value.NormalizedValue == normalized);
            if (row is not null) row.Status = (int)BarcodeRegistryStatus.Released;
            return Task.CompletedTask;
        }).ConfigureAwait(false);
    }

    private static string? Normalize(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static BarcodeRegistryEntry ToDomain(JsonBarcodeRegistryRow row)
        => new(
            row.BarcodeId,
            new Barcode(row.Value, (BarcodeSymbology)row.Symbology),
            (BarcodeRegistryStatus)row.Status,
            row.OwnerKind is int kind ? (BarcodeOwnerKind)kind : null,
            row.OwnerId,
            string.IsNullOrWhiteSpace(row.OwnerName) ? null : row.OwnerName);
}
