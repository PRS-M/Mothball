using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreApp.Entities.ItemAggregate;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.Mappers;
using Infrastructure.Services.Repositories;
using CoreApp.Specifications;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonItemRepository : IItemRepository
{
    private readonly JsonInventoryStore store;

    public JsonItemRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    /// <inheritdoc />
    public async Task<Item?> GetWithPhotosAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var iid)) return null;

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Items.FirstOrDefault(i => i.ItemId == iid);
        if (row is null) return null;

        return MapItem(state, row);
    }

    private async Task<List<Item>> GetAllWithPhotosAsync()
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Items.OrderBy(i => i.RowId).Select(i => MapItem(state, i)).ToList();
    }

    private async Task<List<Item>> GetAllWithPhotosAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Items
            .OrderBy(i => i.RowId)
            .Skip(offset)
            .Take(pageSize)
            .Select(i => MapItem(state, i))
            .ToList();
    }

    public Task<List<Item>> QueryWithPhotosAsync(ItemListSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);
        var hasPaging = RepositoryQueryHelpers.TryGetPaging(
            specification.PageNumber,
            specification.PageSize,
            out var pageNumberValue,
            out var pageSizeValue);

        if (hasSearch)
        {
            return specification.Filter switch
            {
                ItemQueryFilter.Assigned => SearchAssignedWithPhotosAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
                ItemQueryFilter.Unassigned => SearchUnassignedWithPhotosAsync(
                    term!,
                    specification.ExcludedContainerId,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
                _ => SearchWithPhotosAsync(
                    term!,
                    hasPaging ? pageNumberValue : null,
                    hasPaging ? pageSizeValue : null),
            };
        }

        if (hasPaging)
        {
            return specification.Filter switch
            {
                ItemQueryFilter.Assigned => GetAssignedWithPhotosAsync(pageNumberValue, pageSizeValue),
                ItemQueryFilter.Unassigned => GetUnassignedWithPhotosAsync(
                    pageNumberValue,
                    pageSizeValue,
                    specification.ExcludedContainerId),
                _ => GetAllWithPhotosAsync(pageNumberValue, pageSizeValue),
            };
        }

        return specification.Filter switch
        {
            ItemQueryFilter.Assigned => SearchAssignedWithPhotosAsync(string.Empty),
            ItemQueryFilter.Unassigned => SearchUnassignedWithPhotosAsync(string.Empty, specification.ExcludedContainerId),
            _ => GetAllWithPhotosAsync(),
        };
    }

    public Task<List<Item>> QueryContainerItemsWithPhotosAsync(ContainerItemsSpecification specification)
    {
        var (term, hasSearch) = NormalizeSearch(specification.SearchTerm);
        var hasPaging = RepositoryQueryHelpers.TryGetPaging(
            specification.PageNumber,
            specification.PageSize,
            out var pageNumberValue,
            out var pageSizeValue);

        if (hasSearch)
        {
            return SearchItemsInContainerAsync(
                specification.ContainerId,
                term!,
                hasPaging ? pageNumberValue : 0,
                hasPaging ? pageSizeValue : int.MaxValue);
        }

        return GetItemsForContainerAsync(
            specification.ContainerId,
            hasPaging ? pageNumberValue : null,
            hasPaging ? pageSizeValue : null);
    }

    private async Task<List<Item>> GetItemsForContainerAsync(string containerId, int? pageNumber = null, int? pageSize = null)
    {
        if (!Guid.TryParse(containerId, out var cid)) return [];

        var state = await store.LoadAsync().ConfigureAwait(false);
        var relationIds = state.Relations
            .Where(r => r.ContainerId == cid && r.Quantity > 0)
            .GroupBy(r => r.ItemId)
            .Select(group => group.OrderBy(r => r.Id).First())
            .OrderBy(r => r.Id);
        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            relationIds = relationIds
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue)
                .OrderBy(r => r.Id);
        }

        var ids = relationIds.Select(r => r.ItemId).ToList();
        var itemsById = state.Items
            .Where(i => ids.Contains(i.ItemId))
            .ToDictionary(i => i.ItemId);

        return ids
            .Where(itemsById.ContainsKey)
            .Select(i => MapItem(state, itemsById[i]))
            .ToList();
    }

    private async Task<List<Item>> GetUnassignedWithPhotosAsync(
        int pageNumber,
        int pageSize,
        Guid? excludedContainerId = null)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Items
            .Where(i => GetTotalQuantity(state, i.ItemId) > GetAssignedQuantity(state, i.ItemId)
                && !HasPositiveAllocationInContainer(state, i.ItemId, excludedContainerId))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.RowId)
            .Skip(offset)
            .Take(pageSize)
            .Select(i => MapItem(state, i))
            .ToList();
    }

    private async Task<List<Item>> GetAssignedWithPhotosAsync(int pageNumber, int pageSize)
    {
        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Items
            .Where(i => GetAssignedQuantity(state, i.ItemId) > 0)
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.RowId)
            .Skip(offset)
            .Take(pageSize)
            .Select(i => MapItem(state, i))
            .ToList();
    }

    private async Task<List<Item>> SearchWithPhotosAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var query = state.Items
            .Where(i => i.Name.Contains(searchTerm ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.RowId);

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            query = query
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue)
                .OrderBy(i => i.RowId);
        }

        return query
            .Select(i => MapItem(state, i))
            .ToList();
    }

    private async Task<List<Item>> SearchUnassignedWithPhotosAsync(
        string searchTerm,
        Guid? excludedContainerId = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var query = state.Items
            .Where(i => GetTotalQuantity(state, i.ItemId) > GetAssignedQuantity(state, i.ItemId)
                && !HasPositiveAllocationInContainer(state, i.ItemId, excludedContainerId)
                && i.Name.Contains(searchTerm ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.RowId);

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            query = query
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue)
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.RowId);
        }

        return query
            .Select(i => MapItem(state, i))
            .ToList();
    }

    private async Task<List<Item>> SearchAssignedWithPhotosAsync(
        string searchTerm,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var query = state.Items
            .Where(i => GetAssignedQuantity(state, i.ItemId) > 0
                && i.Name.Contains(searchTerm ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.RowId);

        if (RepositoryQueryHelpers.TryGetPaging(pageNumber, pageSize, out var pageNumberValue, out var pageSizeValue))
        {
            query = query
                .Skip(RepositoryQueryHelpers.CalculateOffset(pageNumberValue, pageSizeValue))
                .Take(pageSizeValue)
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.RowId);
        }

        return query
            .Select(i => MapItem(state, i))
            .ToList();
    }

    private async Task<List<Item>> SearchItemsInContainerAsync(string containerId, string searchTerm, int pageNumber, int pageSize)
    {
        if (!Guid.TryParse(containerId, out var cid)) return [];

        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);

        // Container-item search is allocation based: duplicate physical relation rows still produce one item row.
        var matches = state.Relations
            .Where(r => r.ContainerId == cid && r.Quantity > 0)
            .GroupBy(r => r.ItemId)
            .Select(group => group.OrderBy(r => r.Id).First())
            .OrderBy(r => r.Id)
            .Select(r => state.Items.FirstOrDefault(i => i.ItemId == r.ItemId))
            .Where(i => i is not null)
            .Select(i => i!)
            .Where(i => i.Name.Contains(searchTerm ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            .Skip(offset)
            .Take(pageSize)
            .Select(i => MapItem(state, i))
            .ToList();

        return matches;
    }

    /// <inheritdoc />
    public Task InsertAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return store.UpdateAsync(state =>
        {
            var existing = state.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (existing is not null)
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                return Task.CompletedTask;
            }

            state.Items.Add(new JsonItemRow
            {
                RowId = state.Metadata.NextItemRowId++,
                ItemId = item.ItemId,
                Name = item.Name,
                Description = item.Description,
            });

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task UpdateAsync(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return store.UpdateAsync(state =>
        {
            var existing = state.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (existing is null)
            {
                state.Items.Add(new JsonItemRow
                {
                    RowId = state.Metadata.NextItemRowId++,
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                });
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task DeletePhotoAsync(Item item, Guid imageId)
    {
        ArgumentNullException.ThrowIfNull(item);

        return store.UpdateAsync(state =>
        {
            state.Images.RemoveAll(i => i.ImageId == imageId && i.OwnerUniqueId == item.ItemId);

            var existing = state.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (existing is null)
            {
                state.Items.Add(new JsonItemRow
                {
                    RowId = state.Metadata.NextItemRowId++,
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                });
            }
            else
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
            }

            return Task.CompletedTask;
        });
    }

    /// <inheritdoc />
    public Task DeleteAsync(string itemId)
    {
        if (!Guid.TryParse(itemId, out var iid)) return Task.CompletedTask;

        return store.UpdateAsync(state =>
        {
            state.Images.RemoveAll(p => p.OwnerUniqueId == iid);
            state.Relations.RemoveAll(r => r.ItemId == iid);
            state.Inventories.RemoveAll(i => i.ItemId == iid);
            state.Items.RemoveAll(i => i.ItemId == iid);
            return Task.CompletedTask;
        });
    }

    private static Item MapItem(JsonInventoryStore.StoreState state, JsonItemRow row)
    {
        var dbItem = new DbItem
        {
            ItemId = row.ItemId,
            Name = row.Name,
            Description = row.Description,
        };

        var photos = state.Images
            .Where(p => p.OwnerUniqueId == row.ItemId)
            .OrderBy(p => p.RowId)
            .Select(p => new DbImage { ImageId = p.ImageId, OwnerUniqueId = p.OwnerUniqueId })
            .ToList();

        return dbItem.ToDomain(photos);
    }

    private static int GetAssignedQuantity(JsonInventoryStore.StoreState state, Guid itemId)
        => state.Relations
            .Where(relation => relation.ItemId == itemId && relation.Quantity > 0)
            .Sum(relation => relation.Quantity);

    private static int GetTotalQuantity(JsonInventoryStore.StoreState state, Guid itemId)
        => state.Inventories.FirstOrDefault(inventory => inventory.ItemId == itemId)?.TotalQuantity ?? 1;

    private static bool HasPositiveAllocationInContainer(
        JsonInventoryStore.StoreState state,
        Guid itemId,
        Guid? containerId)
        => containerId is Guid cid
            && state.Relations.Any(relation =>
                relation.ItemId == itemId
                && relation.ContainerId == cid
                && relation.Quantity > 0);

    private static (string? term, bool hasSearch) NormalizeSearch(string? searchTerm)
    {
        var term = searchTerm?.Trim();
        return (term, !string.IsNullOrWhiteSpace(term));
    }
}
