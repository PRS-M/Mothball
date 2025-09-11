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
    private readonly IRepository<DbContainer> containers;
    private readonly IRepository<DbItem> items;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly IFileHandler fileHandler;

    public DemoDataSeeder(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        IFileHandler fileHandler)
    {
        this.containers = containers;
        this.items = items;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.fileHandler = fileHandler;
    }

    /// <summary>
    /// Ensures at least <paramref name="minContainers"/> containers exist, optionally with one photo each.
    /// </summary>
    public async Task EnsureContainersAsync(int minContainers = 5, bool withPhotos = true)
    {
    await containers.InitializeAsync();
    await photos.InitializeAsync();

    var existing = await containers.GetAllAsync();
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

            await containers.InsertAsync(container);

            if (withPhotos)
            {
                var img = new DbImage
                {
                    // ImageId auto-generated
                    OwnerUniqueId = id,
                    ImageData = null // keep on disk only; UI will fallback if file isn't present
                };

                await photos.InsertAsync(img);
                await fileHandler.CopyFileFromRawToAppDataAsync("container.png", img.FileName, Constants.PathToContainerPhotos);
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
    await containers.InitializeAsync();
    await items.InitializeAsync();
    await photos.InitializeAsync();
    await itemContainerRelations.InitializeAsync();

        // Make sure we have some containers to attach items to
        var containersList = await containers.GetAllAsync();
        if (containersList.Count == 0)
        {
            await EnsureContainersAsync(minContainers: 3, withPhotos: true);
            containersList = await containers.GetAllAsync();
        }

        foreach (var container in containersList)
        {
            var relationsForContainer = await itemContainerRelations.WhereAsync(r => r.ContainerId == container.ContainerId);
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

                await items.InsertAsync(item);

                // Create relation
                var relation = new DbItemContainerRelation
                {
                    ItemId = itemId,
                    ContainerId = container.ContainerId
                };
                await itemContainerRelations.InsertAsync(relation);

                if (withPhotos)
                {
                    var img = new DbImage
                    {
                        OwnerUniqueId = itemId,
                        ImageData = null
                    };

                    await photos.InsertAsync(img);

                    // Use a bundled placeholder image; fall back gracefully if missing
                    try
                    {
                        await fileHandler.CopyFileFromRawToAppDataAsync("dotnet_bot.png", img.FileName, Constants.PathToItemPhotos);
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
