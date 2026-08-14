using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreApp.Entities.ContainerAggregate;
using Infrastructure.Interfaces;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.Mappers;
using Infrastructure.Services.Repositories;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonContainerRepository : IContainerRepository
{
    private readonly JsonInventoryStore store;

    public JsonContainerRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    public async Task<Container?> GetAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return null;

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Containers.FirstOrDefault(c => c.ContainerId == cid);
        if (row is null) return null;

        return MapContainer(state, row, includeRelations: true);
    }

    public async Task<List<Container>> GetAllAsync()
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Containers
            .OrderBy(c => c.RowId)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    public async Task<List<Container>> GetAllAsync(int pageNumber, int pageSize)
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

    public async Task<List<Container>> GetEmptyAsync(int pageNumber, int pageSize)
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

    public async Task<List<Container>> SearchAsync(string searchTerm)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var term = searchTerm ?? string.Empty;

        return state.Containers
            .Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Notes.Contains(term, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.RowId)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    public async Task<List<Container>> SearchEmptyAsync(string searchTerm)
    {
        var state = await store.LoadAsync().ConfigureAwait(false);
        var term = searchTerm ?? string.Empty;
        var nonEmpty = state.Relations.Select(r => r.ContainerId).ToHashSet();

        return state.Containers
            .Where(c => !nonEmpty.Contains(c.ContainerId)
                && (c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                || c.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.RowId)
            .Select(c => MapContainer(state, c, includeRelations: true)!)
            .ToList();
    }

    public async Task<Container?> GetWithItemsAndPhotosAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return null;

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Containers.FirstOrDefault(c => c.ContainerId == cid);
        if (row is null) return null;

        return MapContainer(state, row, includeRelations: true);
    }

    public async Task<Container?> GetWithItemsAndPhotosAsync(string containerId, int pageNumber, int pageSize)
    {
        if (!Guid.TryParse(containerId, out var cid)) return null;

        RepositoryQueryHelpers.ValidatePaging(pageNumber, pageSize);
        int offset = RepositoryQueryHelpers.CalculateOffset(pageNumber, pageSize);

        var state = await store.LoadAsync().ConfigureAwait(false);
        var row = state.Containers.FirstOrDefault(c => c.ContainerId == cid);
        if (row is null) return null;

        // Mimic current SQLite behavior: load all relations then apply paging.
        var allRelations = state.Relations.Where(r => r.ContainerId == cid).OrderBy(r => r.Id).ToList();
        var paginatedRelations = allRelations.Skip(offset).Take(pageSize).ToList();

        return MapContainer(state, row, includeRelations: false, overrideRelations: paginatedRelations);
    }

    public async Task<int> GetItemCountInContainerAsync(string containerId)
    {
        if (!Guid.TryParse(containerId, out var cid)) return 0;

        var state = await store.LoadAsync().ConfigureAwait(false);
        return state.Relations.Where(r => r.ContainerId == cid).Sum(r => r.Quantity);
    }

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
                return Task.CompletedTask;
            }

            state.Containers.Add(new JsonContainerRow
            {
                RowId = state.Metadata.NextContainerRowId++,
                ContainerId = container.ContainerId,
                Name = container.Name,
                Notes = container.Notes,
            });

            return Task.CompletedTask;
        });
    }

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
                });
            }
            else
            {
                existing.Name = container.Name;
                existing.Notes = container.Notes;
            }

            return Task.CompletedTask;
        });
    }

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
                });
            }
            else
            {
                existing.Name = container.Name;
                existing.Notes = container.Notes;
            }

            return Task.CompletedTask;
        });
    }

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
}
