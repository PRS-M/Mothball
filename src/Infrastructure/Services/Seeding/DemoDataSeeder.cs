using CoreApp.Application.Utilities;
using CoreApp.Application.Contracts;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.Entities.TagAggregate;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.DatabaseModels;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Seeding;

/// <summary>
/// Development-only data seeder to populate the SQLite database with demo content.
/// Keeps seeding logic in infrastructure to avoid leaking persistence details into UI.
/// </summary>
public class DemoDataSeeder
{
    private const string SharedSeedContainerPhotoFileName = "seeded-container.jpg";
    private const string SharedSeedItemPhotoFileName = "seeded-item.jpg";
    /// <summary>
    /// Identifies the demo data shape represented by this seeder.
    /// </summary>
    public const string SeedVersion = "100-containers-100-items-10-tags-v1";

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
    private readonly IBarcodeRegistryService? barcodeRegistry;
    private readonly ITagRepository? tagRepository;
    private bool sharedSeedContainerPhotoPrepared;
    private bool sharedSeedItemPhotoPrepared;

    public DemoDataSeeder(
        IRepository<DbContainer> containers,
        IRepository<DbItem> items,
        IRepository<DbItemInventory> inventories,
        IRepository<DbImage> photos,
        IRepository<DbItemContainerRelation> itemContainerRelations,
        IFileHandler fileHandler,
        ILogger<DemoDataSeeder> logger,
        IBarcodeRegistryService? barcodeRegistry = null,
        ITagRepository? tagRepository = null)
    {
        this.containers = containers;
        this.items = items;
        this.inventories = inventories;
        this.photos = photos;
        this.itemContainerRelations = itemContainerRelations;
        this.fileHandler = fileHandler;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.barcodeRegistry = barcodeRegistry;
        this.tagRepository = tagRepository;
    }

    /// <summary>
    /// Ensures at least <paramref name="minContainers"/> containers exist, optionally with one photo each.
    /// </summary>
    /// <param name="minContainers">The minimum number of demo containers to ensure.</param>
    /// <param name="withPhotos">Whether each newly created container receives a demo photo.</param>
    public async Task EnsureContainersAsync(
        int minContainers = 100,
        bool withPhotos = true,
        IProgress<double>? progress = null)
    {
        await containers.InitializeAsync();
        await photos.InitializeAsync();

        var existing = await containers.GetAllAsync();
        if (withPhotos)
        {
            sharedSeedContainerPhotoPrepared = await EnsureSharedSeedPhotoAsync(
                "container.png",
                SharedSeedContainerPhotoFileName,
                sharedSeedContainerPhotoPrepared);
        }

        if (existing.Count >= minContainers)
        {
            progress?.Report(1);
            return;
        }

        int toCreate = minContainers - existing.Count;

        for (int i = 0; i < toCreate; i++)
        {
            var id = Guid.NewGuid();
            var container = new DbContainer
            {
                ContainerId = id,
                Name = $"Container {existing.Count + i + 1}",
                Notes = BuildSeedContainerNotes(id),
            };
            SetGeneratedBarcode(container);

            await containers.InsertAsync(container);

            if (withPhotos)
            {
                var img = new DbImage
                {
                    // ImageId auto-generated
                    OwnerUniqueId = id,
                    ImageData = null,
                    StoredFileName = SharedSeedContainerPhotoFileName,
                    IsSharedAsset = true,
                };

                await photos.InsertAsync(img);
            }

            progress?.Report((i + 1d) / toCreate);
        }
    }

    /// <summary>
    /// Performs a lightweight check that the expected demo records are still present.
    /// </summary>
    /// <param name="minContainers">The minimum number of seeded containers expected.</param>
    /// <param name="minItemsPerContainer">The minimum number of seeded items expected in each container.</param>
    /// <returns><see langword="true"/> when the seeded records appear intact.</returns>
    public async Task<bool> IsSeedDataIntactAsync(
        int minContainers = 100,
        int minItemsPerContainer = 100)
    {
        await containers.InitializeAsync();
        await items.InitializeAsync();
        await photos.InitializeAsync();

        var seededContainers = (await containers.GetAllAsync())
            .Where(IsSeedContainer)
            .ToList();

        if (seededContainers.Count < minContainers)
        {
            return false;
        }

        if (!fileHandler.FileExists(SharedSeedContainerPhotoFileName, Constants.PathToSharedPhotos) ||
            !fileHandler.FileExists(SharedSeedItemPhotoFileName, Constants.PathToSharedPhotos))
        {
            return false;
        }

        foreach (var container in seededContainers)
        {
            if (string.IsNullOrWhiteSpace(container.BarcodeValue))
            {
                return false;
            }

            var seededItemPrefix = $"Item {container.Name}-";
            var itemCount = await items.CountAsync(item => item.Name.StartsWith(seededItemPrefix));
            if (itemCount < minItemsPerContainer)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Ensures each existing container has at least <paramref name="minItemsPerContainer"/> items.
    /// Also seeds one photo per item when <paramref name="withPhotos"/> is true.
    /// </summary>
    /// <param name="minItemsPerContainer">The minimum number of demo items for each seeded container.</param>
    /// <param name="withPhotos">Whether each newly created item receives a demo photo.</param>
    public async Task EnsureItemsAsync(
        int minItemsPerContainer = 100,
        bool withPhotos = true,
        IProgress<double>? progress = null)
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
            await EnsureContainersAsync(minContainers: 100, withPhotos: withPhotos);
            containersList = await containers.GetAllAsync();
        }

        // Keep demo seeding scoped to demo-generated containers so user-created
        // containers stay empty until users add items explicitly.
        var seededContainers = containersList
            .Where(IsSeedContainer)
            .ToList();

        if (seededContainers.Count == 0)
        {
            progress?.Report(1);
            return;
        }

        progress?.Report(0);

        var demoTags = await EnsureDemoTagsAsync();
        var orderedSeededContainers = seededContainers
            .OrderBy(GetSeedContainerNumber)
            .ThenBy(container => container.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allItems = await items.GetAllAsync();
        var allInventories = await inventories.GetAllAsync();
        var allRelations = await itemContainerRelations.GetAllAsync();
        var allPhotos = await photos.GetAllAsync();

        if (withPhotos)
        {
            sharedSeedItemPhotoPrepared = await EnsureSharedSeedPhotoAsync(
                "mothball_logo.png",
                SharedSeedItemPhotoFileName,
                sharedSeedItemPhotoPrepared);
        }

        for (var containerIndex = 0; containerIndex < orderedSeededContainers.Count; containerIndex++)
        {
            var container = orderedSeededContainers[containerIndex];
            await EnsureGeneratedBarcodeAsync(container);

            await RemoveDuplicateSeedItemsAsync(
                container,
                allItems,
                allInventories,
                allRelations,
                allPhotos);

            var existingSeededItemNames = allItems
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
                SetGeneratedBarcode(item);

                await items.InsertAsync(item);
                allItems.Add(item);
                existingSeededItemNames.Add(itemName);

                var inventory = new DbItemInventory
                {
                    ItemId = itemId,
                    TotalQuantity = 1,
                };
                await inventories.InsertAsync(inventory);
                allInventories.Add(inventory);

                // Create relation
                var relation = new DbItemContainerRelation
                {
                    ItemId = itemId,
                    ContainerId = container.ContainerId
                };
                await itemContainerRelations.InsertAsync(relation);
                allRelations.Add(relation);

                if (withPhotos)
                {
                    var img = new DbImage
                    {
                        OwnerUniqueId = itemId,
                        ImageData = null,
                        StoredFileName = SharedSeedItemPhotoFileName,
                        IsSharedAsset = true,
                    };

                    await photos.InsertAsync(img);
                    allPhotos.Add(img);

                }
            }

            foreach (var item in allItems.Where(item => IsSeedItemForContainer(item, container)))
            {
                await EnsureGeneratedBarcodeAsync(item);
            }

            if (tagRepository is not null)
            {
                var containerNumber = GetSeedContainerNumber(container);
                await tagRepository.AssignAsync(
                    demoTags[(containerNumber - 1) / 10 % demoTags.Count].TagId,
                    TagTargetType.Container,
                    container.ContainerId);

                var containerItems = allItems
                    .Where(item => IsSeedItemForContainer(item, container))
                    .OrderBy(item => GetSeedItemNumber(item, container))
                    .ToList();
                foreach (var item in containerItems)
                {
                    var itemNumber = GetSeedItemNumber(item, container);
                    await tagRepository.AssignAsync(
                        demoTags[(itemNumber - 1) / 10 % demoTags.Count].TagId,
                        TagTargetType.Item,
                        item.ItemId);
                }
            }

            progress?.Report((containerIndex + 1d) / orderedSeededContainers.Count);
        }
    }

    private async Task<IReadOnlyList<Tag>> EnsureDemoTagsAsync()
    {
        if (tagRepository is null)
        {
            return [];
        }

        var tags = new List<CoreApp.Domain.Entities.TagAggregate.Tag>(10);
        for (var index = 1; index <= 10; index++)
        {
            tags.Add(await tagRepository.GetOrCreateAsync(new TagName($"Demo Tag {index}")));
        }

        return tags;
    }

    private void SetGeneratedBarcode(DbContainer container)
    {
        var barcode = BarcodeGenerator.Create(container.ContainerId, BarcodeOwnerKind.Container, BarcodeSymbology.QrCode);
        container.BarcodeValue = barcode.Value;
        container.BarcodeSymbology = (int)barcode.Symbology;
    }

    private void SetGeneratedBarcode(DbItem item)
    {
        var barcode = BarcodeGenerator.Create(item.ItemId, BarcodeOwnerKind.Item, BarcodeSymbology.QrCode);
        item.BarcodeValue = barcode.Value;
        item.BarcodeSymbology = (int)barcode.Symbology;
    }

    private async Task EnsureGeneratedBarcodeAsync(DbContainer container)
    {
        if (string.IsNullOrWhiteSpace(container.BarcodeValue))
        {
            SetGeneratedBarcode(container);
            await containers.UpdateAsync(container);
        }

        if (barcodeRegistry is not null && !string.IsNullOrWhiteSpace(container.BarcodeValue))
        {
            await barcodeRegistry.AssignAsync(
                new Barcode(container.BarcodeValue, (BarcodeSymbology)(container.BarcodeSymbology ?? (int)BarcodeSymbology.QrCode)),
                BarcodeOwnerKind.Container,
                container.ContainerId,
                container.Name);
        }
    }

    private async Task EnsureGeneratedBarcodeAsync(DbItem item)
    {
        if (string.IsNullOrWhiteSpace(item.BarcodeValue))
        {
            SetGeneratedBarcode(item);
            await items.UpdateAsync(item);
        }

        if (barcodeRegistry is not null && !string.IsNullOrWhiteSpace(item.BarcodeValue))
        {
            await barcodeRegistry.AssignAsync(
                new Barcode(item.BarcodeValue, (BarcodeSymbology)(item.BarcodeSymbology ?? (int)BarcodeSymbology.QrCode)),
                BarcodeOwnerKind.Item,
                item.ItemId,
                item.Name);
        }
    }

    private async Task<bool> EnsureSharedSeedPhotoAsync(
        string rawFileName,
        string sharedFileName,
        bool prepared)
    {
        if (prepared)
        {
            return true;
        }

        if (!fileHandler.FileExists(sharedFileName, Constants.PathToSharedPhotos))
        {
            await fileHandler.CopyFileFromRawToAppDataAsync(
                rawFileName,
                sharedFileName,
                Constants.PathToSharedPhotos);
        }

        return true;
    }

    private async Task RemoveDuplicateSeedItemsAsync(
        DbContainer container,
        List<DbItem> allItems,
        List<DbItemInventory> allInventories,
        List<DbItemContainerRelation> allRelations,
        List<DbImage> allPhotos)
    {
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
                    allRelations.Remove(relation);
                }

                foreach (var photo in allPhotos.Where(photo => photo.OwnerUniqueId == duplicate.ItemId).ToList())
                {
                    await photos.DeleteAsync(photo);
                    allPhotos.Remove(photo);
                }

                var inventory = allInventories.FirstOrDefault(inventory => inventory.ItemId == duplicate.ItemId);
                if (inventory is not null)
                {
                    await inventories.DeleteAsync(inventory);
                    allInventories.Remove(inventory);
                }

                await items.DeleteAsync(duplicate);
                allItems.Remove(duplicate);
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

    private static int GetSeedContainerNumber(DbContainer container)
        => ParseTrailingNumber(container.Name, fallback: 1);

    private static int GetSeedItemNumber(DbItem item, DbContainer container)
        => ParseTrailingNumber(item.Name, fallback: 1);

    private static int ParseTrailingNumber(string value, int fallback)
    {
        var separator = value.LastIndexOf('-');
        return separator >= 0 && int.TryParse(value[(separator + 1)..], out var number) && number > 0
            ? number
            : fallback;
    }

    private static string BuildSeedContainerNotes(Guid containerId)
    {
        return $"{SeededContainerNotesPrefix} {containerId.ToString()[..8]} {GetSeedMarkerToken()}";
    }

    private static string GetSeedMarkerToken()
    {
        return $"{SeedContainerMarkerTokenPrefix}{SeedContainerMarkerGuid:D}]";
    }
}
