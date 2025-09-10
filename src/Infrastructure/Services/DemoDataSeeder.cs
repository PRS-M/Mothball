using System;
using CoreApp.Interfaces;
using CoreApp.Utilities;
using Infrastructure.Interfaces;
using MothballMobile.Infrastructure.DatabaseModels;

namespace MothballMobile.Infrastructure;

/// <summary>
/// Development-only data seeder to populate the SQLite database with demo content.
/// Keeps seeding logic in infrastructure to avoid leaking persistence details into UI.
/// </summary>
public class DemoDataSeeder
{
    private readonly IRepository<DbContainer> _containers;
    private readonly IRepository<DbItem> _items;
    private readonly IRepository<DbImage> _photos;
    private readonly IRepository<DbItemContainerRelation> _itemContainerRelations;
    private readonly IFileHandler _fileHandler;

    public DemoDataSeeder(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        IFileHandler fileHandler)
    {
        _containers = containers;
        _items = items;
        _photos = photos;
        _itemContainerRelations = itemContainerRelations;
        _fileHandler = fileHandler;
    }

    /// <summary>
    /// Ensures at least <paramref name="minContainers"/> containers exist, optionally with one photo each.
    /// </summary>
    public async Task EnsureContainersAsync(int minContainers = 5, bool withPhotos = true)
    {
        await _containers.InitializeAsync();
        await _photos.InitializeAsync();

        var existing = await _containers.GetAllAsync();
        if (existing.Count >= minContainers) return;

        int toCreate = minContainers - existing.Count;

        for (int i = 0; i < toCreate; i++)
        {
            var id = Guid.NewGuid();
            var container = new DbContainer
            {
                ContainerId = id,
                Name = $"Container {existing.Count + i + 1}",
                Notes = $"Seeded notes for container {id.ToString()[..8]}"
            };

            await _containers.InsertAsync(container);

            if (withPhotos)
            {
                var img = new DbImage
                {
                    // ImageId auto-generated
                    OwnerUniqueId = id,
                    ImageData = null // keep on disk only; UI will fallback if file isn't present
                };

                await _photos.InsertAsync(img);
                await _fileHandler.CopyFileFromRawToAppDataAsync("container.png", img.FileName, Constants.PathToContainerPhotos);
            }
        }
    }

    /// <summary>
    /// Ensures each existing container has at least <paramref name="minItemsPerContainer"/> items.
    /// Also seeds one photo per item when <paramref name="withPhotos"/> is true.
    /// </summary>
    public async Task EnsureItemsAsync(int minItemsPerContainer = 3, bool withPhotos = true)
    {
        // Ensure tables exist
        await _containers.InitializeAsync();
        await _items.InitializeAsync();
        await _photos.InitializeAsync();
        await _itemContainerRelations.InitializeAsync();

        // Make sure we have some containers to attach items to
        var containers = await _containers.GetAllAsync();
        if (containers.Count == 0)
        {
            await EnsureContainersAsync(minContainers: 3, withPhotos: true);
            containers = await _containers.GetAllAsync();
        }

        foreach (var container in containers)
        {
            var relationsForContainer = await _itemContainerRelations.WhereAsync(r => r.ContainerId == container.ContainerId);
            int currentCount = relationsForContainer.Count;
            if (currentCount >= minItemsPerContainer) continue;

            int toCreate = minItemsPerContainer - currentCount;
            for (int i = 0; i < toCreate; i++)
            {
                var itemId = Guid.NewGuid();
                var item = new DbItem
                {
                    ItemId = itemId,
                    Name = $"Item {container.Name}-{(currentCount + i + 1)}"
                };

                await _items.InsertAsync(item);

                // Create relation
                var relation = new DbItemContainerRelation
                {
                    ItemId = itemId,
                    ContainerId = container.ContainerId
                };
                await _itemContainerRelations.InsertAsync(relation);

                if (withPhotos)
                {
                    var img = new DbImage
                    {
                        OwnerUniqueId = itemId,
                        ImageData = null
                    };

                    await _photos.InsertAsync(img);

                    // Use a bundled placeholder image; fall back gracefully if missing
                    try
                    {
                        await _fileHandler.CopyFileFromRawToAppDataAsync("dotnet_bot.png", img.FileName, Constants.PathToItemPhotos);
                    }
                    catch
                    {
                        // Ignore copy errors in demo seeding; UI will use its own fallback
                    }
                }
            }
        }
    }
}
