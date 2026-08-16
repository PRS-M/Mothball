using System;
using System.Linq;
using System.Threading.Tasks;
using CoreApp.Entities.Shared;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore.Repositories;

public sealed class JsonImageRepository : IImageRepository
{
    private readonly JsonInventoryStore store;

    public JsonImageRepository(JsonInventoryStore store)
    {
        this.store = store;
    }

    public Task InsertAsync(ImageItem imageItem, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(imageItem);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        return store.UpdateAsync(state =>
        {
            var existing = state.Images.FirstOrDefault(i => i.ImageId == imageItem.ImageId);
            if (existing is null)
            {
                state.Images.Add(new JsonImageRow
                {
                    RowId = state.Metadata.NextImageRowId++,
                    ImageId = imageItem.ImageId,
                    OwnerUniqueId = ownerId,
                    ImageDataBase64 = null,
                });
            }
            else
            {
                existing.OwnerUniqueId = ownerId;
            }

            return Task.CompletedTask;
        });
    }

    public Task UpdateAsync(ImageItem image, Guid ownerId)
    {
        ArgumentNullException.ThrowIfNull(image);
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        // Current SQLite update effectively upserts by PK.
        return store.UpdateAsync(state =>
        {
            var existing = state.Images.FirstOrDefault(i => i.ImageId == image.ImageId);
            if (existing is null)
            {
                state.Images.Add(new JsonImageRow
                {
                    RowId = state.Metadata.NextImageRowId++,
                    ImageId = image.ImageId,
                    OwnerUniqueId = ownerId,
                    ImageDataBase64 = null,
                });
            }
            else
            {
                existing.OwnerUniqueId = ownerId;
            }

            return Task.CompletedTask;
        });
    }

    public Task DeleteAsync(Guid imageId, Guid ownerId)
    {
        if (ownerId == Guid.Empty) throw new ArgumentException("Owner ID cannot be empty.", nameof(ownerId));

        return store.UpdateAsync(state =>
        {
            state.Images.RemoveAll(i => i.ImageId == imageId && i.OwnerUniqueId == ownerId);
            return Task.CompletedTask;
        });
    }
}
