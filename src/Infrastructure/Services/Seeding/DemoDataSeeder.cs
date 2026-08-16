using System;
using CoreApp.Utilities;
using Infrastructure.Services.DatabaseModels;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Seeding;

/// <summary>
/// Development-only data seeder to populate the SQLite database with demo content.
/// Keeps seeding logic in infrastructure to avoid leaking persistence details into UI.
/// </summary>
public class DemoDataSeeder
{
    private const string SeededContainerNotesPrefix = "Seeded notes for container";
    private const string SeedContainerMarkerTokenPrefix = "[SEED-CONTAINER-MARKER:";
    private static readonly Guid SeedContainerMarkerGuid = new("4f3c5d11-2f9b-44b3-9e55-2e0f1ea7a8d2");

    private readonly IRepository<DbContainer> containers;
    private readonly IRepository<DbItem> items;
    private readonly IRepository<DbItemInventory> inventories;
    private readonly IRepository<DbImage> photos;
    private readonly IRepository<DbItemContainerRelation> itemContainerRelations;
    private readonly IFileHandler fileHandler;
    private readonly ILogger<DemoDataSeeder> logger;

    public DemoDataSeeder(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbItemInventory> inventories,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        IFileHandler fileHandler,
        ILogger<DemoDataSeeder> logger)
    {
        this.containers = containers;
        this.items = items;
        this.inventories = inventories;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.fileHandler = fileHandler;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                Notes = BuildSeedContainerNotes(id)
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
        await inventories.InitializeAsync();
        await photos.InitializeAsync();
        await itemContainerRelations.InitializeAsync();

        // Make sure we have some containers to attach items to
        var containersList = await containers.GetAllAsync();
        if (containersList.Count == 0)
        {
            await EnsureContainersAsync(minContainers: 3, withPhotos: true);
            containersList = await containers.GetAllAsync();
        }

        // Keep demo seeding scoped to demo-generated containers so user-created
        // containers stay empty until users add items explicitly.
        var seededContainers = containersList
            .Where(IsSeedContainer)
            .ToList();

        if (seededContainers.Count == 0)
        {
            return;
        }

        foreach (var container in seededContainers)
        {
            await RemoveDuplicateSeedItemsAsync(container);

            var existingSeededItemNames = (await items.GetAllAsync())
                .Where(item => IsSeedItemForContainer(item, container))
                .Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            for (int ordinal = 1; ordinal <= minItemsPerContainer; ordinal++)
            {
                var itemName = BuildSeedItemName(container, ordinal);
                if (existingSeededItemNames.Contains(itemName))
                {
                    continue;
                }

                var itemId = Guid.NewGuid();
                var item = new DbItem
                {
                    ItemId = itemId,
                    Name = itemName,
                };

                await items.InsertAsync(item);
                await inventories.InsertAsync(new DbItemInventory
                {
                    ItemId = itemId,
                    TotalQuantity = 1,
                });

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
                        await fileHandler.CopyFileFromRawToAppDataAsync("mothball_logo.png", img.FileName, Constants.PathToItemPhotos);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to copy demo item photo {SourceFileName} to {TargetFileName}.", "mothball_logo.png", img.FileName);
                        // Ignore copy errors in demo seeding; UI will use its own fallback
                    }
                }
            }
        }
    }

    private async Task RemoveDuplicateSeedItemsAsync(DbContainer container)
    {
        var allItems = await items.GetAllAsync();
        var allInventories = await inventories.GetAllAsync();
        var allRelations = await itemContainerRelations.GetAllAsync();
        var allPhotos = await photos.GetAllAsync();

        var duplicateGroups = allItems
            .Where(item => IsSeedItemForContainer(item, container))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();

        foreach (var group in duplicateGroups)
        {
            var keep = group
                .OrderByDescending(item => allInventories
                    .FirstOrDefault(inventory => inventory.ItemId == item.ItemId)?.TotalQuantity ?? 1)
                .ThenByDescending(item => allRelations
                    .Where(relation => relation.ItemId == item.ItemId)
                    .Sum(relation => relation.Quantity))
                .First();

            foreach (var duplicate in group.Where(item => item.ItemId != keep.ItemId))
            {
                foreach (var relation in allRelations.Where(relation => relation.ItemId == duplicate.ItemId).ToList())
                {
                    await itemContainerRelations.DeleteAsync(relation);
                }

                foreach (var photo in allPhotos.Where(photo => photo.OwnerUniqueId == duplicate.ItemId).ToList())
                {
                    await photos.DeleteAsync(photo);
                }

                var inventory = allInventories.FirstOrDefault(inventory => inventory.ItemId == duplicate.ItemId);
                if (inventory is not null)
                {
                    await inventories.DeleteAsync(inventory);
                }

                await items.DeleteAsync(duplicate);
            }
        }
    }

    private static bool IsSeedContainer(DbContainer container)
    {
        if (string.IsNullOrWhiteSpace(container.Notes))
        {
            return false;
        }

        var markerToken = GetSeedMarkerToken();
        return container.Notes.Contains(markerToken, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSeedItemForContainer(DbItem item, DbContainer container)
        => item.Name.StartsWith($"Item {container.Name}-", StringComparison.OrdinalIgnoreCase);

    private static string BuildSeedItemName(DbContainer container, int ordinal)
        => $"Item {container.Name}-{ordinal}";

    private static string BuildSeedContainerNotes(Guid containerId)
    {
        return $"{SeededContainerNotesPrefix} {containerId.ToString()[..8]} {GetSeedMarkerToken()}";
    }

    private static string GetSeedMarkerToken()
    {
        return $"{SeedContainerMarkerTokenPrefix}{SeedContainerMarkerGuid:D}]";
    }
}
