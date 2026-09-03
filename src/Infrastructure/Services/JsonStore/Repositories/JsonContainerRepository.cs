using CoreApp.Domain.Entities.InventoryAggregate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreApp.Domain.Entities.ContainerAggregate;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.Mappers;
using Infrastructure.Services.Repositories;
using CoreApp.Application.Specifications;
using CoreApp.Application.Contracts;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonContainerRepository : IContainerRepository
{
    private readonly JsonInventoryStore store;

    public JsonContainerRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    /// <inheritdoc />
    public async Task<Container?> GetAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return null;

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Containers.FirstOrDefault(c => c.ContainerId == cid);
        if (row is null) return null;

        return MapContainer(state, row, includeRelations: true);
    }

    /// <inheritdoc />
    public async Task<Container?> FindByBarcodeAsync(string barcodeValue)
    {
        var normalizedValue = barcodeValue?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Containers.FirstOrDefault(container => container.BarcodeValue == normalizedValue);
        return row is null ? null : MapContainer(state, row, includeRelations: true);
    }

    private async Task<List<Container>> GetAllAsync()
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Containers
            .OrderBy(c => c.RowId)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    private async Task<List<Container>> GetAllAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Containers
            .OrderBy(c => c.RowId)
            .Skip(offset)
            .Take(pageSize)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    public Task<List<Container>> QueryAsync(ContainerListSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);
        var hasPaging = RepositoryQueryHelpers.TryGetPaging(
            specification.PageNumber,
            specification.PageSize,
            out var pageNumberValue,
            out var pageSizeValue);

        if (hasSearch)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? SearchEmptyAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null)
                : SearchAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null);
        }

        if (hasPaging)
        {
            return specification.Filter == ContainerQueryFilter.Empty
                ? GetEmptyAsync(pageNumberValue, pageSizeValue)
                : GetAllAsync(pageNumberValue, pageSizeValue);
        }

        return specification.Filter == ContainerQueryFilter.Empty
            ? SearchEmptyAsync(string.Empty)
            : GetAllAsync();
    }

    private async Task<List<Container>> GetEmptyAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        var nonEmpty = state.Relations.Select(r => r.ContainerId).ToHashSet();

        return state.Containers
            .Where(c => !nonEmpty.Contains(c.ContainerId))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.RowId)
            .Skip(offset)
            .Take(pageSize)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    private async Task<List<Container>> SearchAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var term = searchTerm ?? string.Empty;
        IEnumerable<JsonContainerRow> query = state.Containers
            .Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.RowId);

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            RepositoryQueryHelpers.ValidatePaging(pageNumberValue, pageSizeValue);
            query = query
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue);
        }

        return query
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    private async Task<List<Container>> SearchEmptyAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var term = searchTerm ?? string.Empty;
        var nonEmpty = state.Relations.Select(r => r.ContainerId).ToHashSet();
        IEnumerable<JsonContainerRow> query = state.Containers
            .Where(c => !nonEmpty.Contains(c.ContainerId)
                && (c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.RowId);

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            RepositoryQueryHelpers.ValidatePaging(pageNumberValue, pageSizeValue);
            query = query
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue);
        }

        return query
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetItemCountInContainerAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return 0;

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Relations.Where(r => r.ContainerId == cid).Sum(r => r.Quantity);
    }

    /// <inheritdoc />
    public async Task<int> GetDistinctItemCountInContainerAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return 0;

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Relations
            .Where(r => r.ContainerId == cid && r.Quantity > 0)
            .Select(r => r.ItemId)
            .Distinct()
            .Count();
    }

    /// <inheritdoc />
    public async Task<Container?> GetContainerForItemAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var iid)) return null;

        var state = await store.LoadAsync().ConfigureAwait(false);
        var relation = state.Relations.Where(r => r.ItemId == iid).OrderBy(r => r.Id).FirstOrDefault();
        if (relation is null) return null;

        var row = state.Containers.FirstOrDefault(c => c.ContainerId == relation.ContainerId);
        if (row is null) return null;

        return MapContainer(state, row, includeRelations: true);
    }

    public async Task<List<ItemContainerAllocation>> GetItemContainerAllocationsAsync(Guid itemId)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Relations
            .Where(relation => relation.ItemId == itemId && relation.Quantity > 0)
            .GroupBy(relation => relation.ContainerId)
            .Select(group =>
            {
                var container = state.Containers.FirstOrDefault(row => row.ContainerId == group.Key);
                return container is null
                    ? null
                    : new ItemContainerAllocation(group.Key, container.Name, group.Sum(row => row.Quantity));
            })
            .Where(allocation => allocation is not null)
            .Select(allocation => allocation!)
            .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<ItemContainerAllocation>>> GetItemContainerAllocationsAsync(
        IReadOnlyCollection<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);

        var distinctItemIds = itemIds.Where(itemId => itemId != Guid.Empty).ToHashSet();
        if (distinctItemIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ItemContainerAllocation>>();
        }

        var state = await store.LoadAsync().ConfigureAwait(false);
        var containersById = state.Containers.ToDictionary(container => container.ContainerId);

        return state.Relations
            .Where(relation => distinctItemIds.Contains(relation.ItemId) && relation.Quantity > 0)
            .GroupBy(relation => new { relation.ItemId, relation.ContainerId })
            .Select(group => new
            {
                group.Key.ItemId,
                group.Key.ContainerId,
                Quantity = group.Sum(relation => relation.Quantity),
            })
            .Where(relation => containersById.ContainsKey(relation.ContainerId))
            .GroupBy(relation => relation.ItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ItemContainerAllocation>)group
                    .Select(relation => new ItemContainerAllocation(
                        relation.ContainerId,
                        containersById[relation.ContainerId].Name,
                        relation.Quantity))
                    .OrderBy(allocation => allocation.ContainerName, StringComparer.OrdinalIgnoreCase)
                    .ToList());
    }

    /// <inheritdoc />
    public Task InsertAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return store.UpdateAsync(state =>
        {
            // If it already exists, treat as update (similar to Insert failure avoidance).
            var existing = state.Containers.FirstOrDefault(c => c.ContainerId == container.ContainerId);
            if (existing is not null)
            {
                existing.Name = container.Name;
                existing.Notes = container.Notes;
                existing.BarcodeValue = container.Barcode?.Value ?? string.Empty;
                existing.BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology;
                return Task.CompletedTask;
            }

            state.Containers.Add(new JsonContainerRow
            {
                RowId = state.Metadata.NextContainerRowId++,
                ContainerId = container.ContainerId,
                Name = container.Name,
                Notes = container.Notes,
                BarcodeValue = container.Barcode?.Value ?? string.Empty,
                BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology,
            });

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task UpdateAsync(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return store.UpdateAsync(state =>
        {
            var existing = state.Containers.FirstOrDefault(c => c.ContainerId == container.ContainerId);
            if (existing is null)
            {
                state.Containers.Add(new JsonContainerRow
                {
                    RowId = state.Metadata.NextContainerRowId++,
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                    BarcodeValue = container.Barcode?.Value ?? string.Empty,
                    BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology,
                });
            }
            else
            {
                existing.Name = container.Name;
                existing.Notes = container.Notes;
                existing.BarcodeValue = container.Barcode?.Value ?? string.Empty;
                existing.BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology;
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task DeletePhotoAsync(Container container, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(container);

        return store.UpdateAsync(state =>
        {
            state.Images.RemoveAll(i => i.ImageId == imageId && i.OwnerUniqueId == container.ContainerId);

            var existing = state.Containers.FirstOrDefault(c => c.ContainerId == container.ContainerId);
            if (existing is null)
            {
                state.Containers.Add(new JsonContainerRow
                {
                    RowId = state.Metadata.NextContainerRowId++,
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                    BarcodeValue = container.Barcode?.Value ?? string.Empty,
                    BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology,
                });
            }
            else
            {
                existing.Name = container.Name;
                existing.Notes = container.Notes;
                existing.BarcodeValue = container.Barcode?.Value ?? string.Empty;
                existing.BarcodeSymbology = container.Barcode is null ? null : (int)container.Barcode.Symbology;
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task DeleteAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return Task.CompletedTask;

        return store.UpdateAsync(state =>
        {
            state.Images.RemoveAll(p => p.OwnerUniqueId == cid);
            state.Relations.RemoveAll(r => r.ContainerId == cid);
            state.Containers.RemoveAll(c => c.ContainerId == cid);
            return Task.CompletedTask;
        });
    }

    private static Container MapContainer(
        JsonInventoryStore.StoreState state,
        JsonContainerRow row,
        bool includeRelations,
        List<JsonRelationRow>? overrideRelations = null)
    {
        var dbContainer = new DbContainer
        {
            ContainerId = row.ContainerId,
            Name = row.Name,
            Notes = row.Notes,
            BarcodeValue = row.BarcodeValue,
            BarcodeSymbology = row.BarcodeSymbology,
        };

        var photos = state.Images
            .Where(p => p.OwnerUniqueId == row.ContainerId)
            .OrderBy(p => p.RowId)
            .Select(p => new DbImage { ImageId = p.ImageId, OwnerUniqueId = p.OwnerUniqueId })
            .ToList();

        List<DbItemContainerRelation>? relations = null;
        if (overrideRelations is not null)
        {
            relations = overrideRelations
                .OrderBy(r => r.Id)
                .Select(r => new DbItemContainerRelation
                {
                    Id = r.Id,
                    ItemId = r.ItemId,
                    ContainerId = r.ContainerId,
                    Quantity = r.Quantity,
                })
                .ToList();
        }
        else if (includeRelations)
        {
            relations = state.Relations
                .Where(r => r.ContainerId == row.ContainerId)
                .OrderBy(r => r.Id)
                .Select(r => new DbItemContainerRelation
                {
                    Id = r.Id,
                    ItemId = r.ItemId,
                    ContainerId = r.ContainerId,
                    Quantity = r.Quantity,
                })
                .ToList();
        }

        return dbContainer.ToDomain(photos, relations);
    }

    private static (string? term, bool hasSearch) NormalizeSearch(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        return (term, !string.IsNullOrWhiteSpace(term));
    }
}
