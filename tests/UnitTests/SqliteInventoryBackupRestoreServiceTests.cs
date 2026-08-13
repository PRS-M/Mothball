using CoreApp.Contracts;
using CoreApp.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.Restore;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Repositories;
using CoreApp.Utilities;

namespace UnitTests;

[TestFixture]
public class SqliteInventoryBackupRestoreServiceTests
{
    private string dbPath = null!;
    private MothballDatabase db = null!;

    [SetUp]
    public async Task Setup()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"mothball-restore-{Guid.NewGuid():N}.db");
        db = new MothballDatabase(dbPath);
        await db.InitializeAsync();
    }

    [TearDown]
    public async Task Teardown()
    {
        try
        {
            if (db != null)
            {
                await db.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            TestContext.Error.WriteLine($"Failed to dispose test database: {ex}");
            // ignore disposal issues in tests
        }

        try
        {
            if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
        catch (Exception ex)
        {
            TestContext.Error.WriteLine($"Failed to delete test database '{dbPath}': {ex}");
            // best effort cleanup
        }
    }

    [Test]
    public async Task RestoreAsync_RollsBackWholeRestore_WhenAnyInsertFails()
    {
        var service = new SqliteInventoryBackupRestoreService(db);

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer
                    {
                        ContainerId = Guid.NewGuid(),
                        Name = "Valid container",
                        Notes = "ok",
                    },
                    // Intentionally invalid for SQLite NOT NULL Name column
                    new InventoryBackupContainer
                    {
                        ContainerId = Guid.NewGuid(),
                        Name = null!,
                        Notes = "bad",
                    },
                ],
                Items = [],
                Relations = [],
                Images = [],
            },
        });

        Assert.ThrowsAsync<SQLite.NotNullConstraintViolationException>(() => service.RestoreAsync(backup));

        var containersRepo = new Repository<DbContainer>(db);
        var rows = await containersRepo.GetAllAsync();
        Assert.That(rows, Is.Empty);
    }

    [Test]
    public async Task RestoreAsync_IsIncremental_AndAddsOnlyMissingData()
    {
        var containerId = Guid.NewGuid();
        var newContainerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var newItemId = Guid.NewGuid();
        var existingContainerImageId = Guid.NewGuid();
        var newContainerImageId = Guid.NewGuid();
        var existingItemImageId = Guid.NewGuid();
        var newItemImageId = Guid.NewGuid();

        var containersRepo = new Repository<DbContainer>(db);
        var itemsRepo = new Repository<DbItem>(db);
        var imagesRepo = new Repository<DbImage>(db);
        var relationsRepo = new Repository<DbItemContainerRelation>(db);

        await containersRepo.InsertAsync(new DbContainer { ContainerId = containerId, Name = "Existing", Notes = "n" });
        await itemsRepo.InsertAsync(new DbItem { ItemId = itemId, Name = "Existing Item", Description = "d" });
        await relationsRepo.InsertAsync(new DbItemContainerRelation { ContainerId = containerId, ItemId = itemId, Quantity = 2 });
        await imagesRepo.InsertAsync(new DbImage { ImageId = existingContainerImageId, OwnerUniqueId = containerId });
        await imagesRepo.InsertAsync(new DbImage { ImageId = existingItemImageId, OwnerUniqueId = itemId });

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "Existing", Notes = "n" },
                    new InventoryBackupContainer { ContainerId = newContainerId, Name = "New", Notes = "new" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = itemId, Name = "Existing Item", Description = "d" },
                    new InventoryBackupItem { ItemId = newItemId, Name = "New Item", Description = "new" },
                ],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = itemId, Quantity = 5 },
                    new InventoryBackupRelation { ContainerId = newContainerId, ItemId = newItemId, Quantity = 1 },
                ],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        ImageId = existingContainerImageId,
                        OwnerId = containerId,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{existingContainerImageId}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = newContainerImageId,
                        OwnerId = containerId,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{newContainerImageId}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = existingItemImageId,
                        OwnerId = itemId,
                        OwnerType = InventoryBackupOwnerType.Item,
                        FileName = $"{existingItemImageId}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = newItemImageId,
                        OwnerId = newItemId,
                        OwnerType = InventoryBackupOwnerType.Item,
                        FileName = $"{newItemImageId}.jpg",
                    },
                ],
            },
        });

        IInventoryBackupRestoreService service = new SqliteInventoryBackupRestoreService(db);
        var result = await service.RestoreAsync(backup);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddedContainers, Is.EqualTo(1));
            Assert.That(result.AddedItems, Is.EqualTo(1));
            Assert.That(result.AddedRelations, Is.EqualTo(2));
            Assert.That(result.AddedRelationQuantity, Is.EqualTo(4));
            Assert.That(result.AddedImages, Is.EqualTo(2));
            Assert.That(result.SkippedExistingContainers, Is.EqualTo(1));
            Assert.That(result.SkippedExistingItems, Is.EqualTo(1));
            Assert.That(result.SkippedExistingImages, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task RestoreAsync_StrictFullSync_ReconcilesRelationsAndImagesExactly()
    {
        var containerId = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var keepImageId = Guid.NewGuid();
        var deleteImageId = Guid.NewGuid();

        var containersRepo = new Repository<DbContainer>(db);
        var itemsRepo = new Repository<DbItem>(db);
        var imagesRepo = new Repository<DbImage>(db);
        var relationsRepo = new Repository<DbItemContainerRelation>(db);

        await containersRepo.InsertAsync(new DbContainer { ContainerId = containerId, Name = "Container", Notes = "Notes" });
        await itemsRepo.InsertAsync(new DbItem { ItemId = item1Id, Name = "Item1", Description = "D1" });
        await itemsRepo.InsertAsync(new DbItem { ItemId = item2Id, Name = "Item2", Description = "D2" });

        await relationsRepo.InsertAsync(new DbItemContainerRelation { ContainerId = containerId, ItemId = item1Id, Quantity = 5 });
        await relationsRepo.InsertAsync(new DbItemContainerRelation { ContainerId = containerId, ItemId = item2Id, Quantity = 1 });

        await imagesRepo.InsertAsync(new DbImage { ImageId = keepImageId, OwnerUniqueId = containerId });
        await imagesRepo.InsertAsync(new DbImage { ImageId = deleteImageId, OwnerUniqueId = containerId });

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "Container", Notes = "Notes" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = item1Id, Name = "Item1", Description = "D1" },
                    new InventoryBackupItem { ItemId = item2Id, Name = "Item2", Description = "D2" },
                ],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = item1Id, Quantity = 2 },
                ],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        ImageId = keepImageId,
                        OwnerId = containerId,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{keepImageId}.jpg",
                    },
                ],
            },
        });

        IInventoryBackupRestoreService service = new SqliteInventoryBackupRestoreService(db);
        var result = await service.RestoreAsync(backup, new InventoryBackupRestoreOptions
        {
            ConflictPolicy = InventoryBackupConflictPolicy.StrictFullSync,
        });

        var remainingItems = await itemsRepo.GetAllAsync();
        var remainingRelations = await relationsRepo.GetAllAsync();
        var remainingImages = await imagesRepo.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(remainingItems.Select(i => i.ItemId), Is.EquivalentTo(new[] { item1Id, item2Id }));
            Assert.That(remainingRelations.Count, Is.EqualTo(1));
            Assert.That(remainingRelations[0].ItemId, Is.EqualTo(item1Id));
            Assert.That(remainingRelations[0].ContainerId, Is.EqualTo(containerId));
            Assert.That(remainingRelations[0].Quantity, Is.EqualTo(2));
            Assert.That(remainingImages.Select(i => i.ImageId), Is.EquivalentTo(new[] { keepImageId }));

            Assert.That(result.DeletedItems, Is.EqualTo(0));
            Assert.That(result.DeletedRelations, Is.EqualTo(1));
            Assert.That(result.DeletedImages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task RestoreAsync_FullSync_KeepsAdditiveRelationsAndImagesForSurvivingRoots()
    {
        var containerId = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var keepImageId = Guid.NewGuid();
        var extraImageId = Guid.NewGuid();

        var containersRepo = new Repository<DbContainer>(db);
        var itemsRepo = new Repository<DbItem>(db);
        var imagesRepo = new Repository<DbImage>(db);
        var relationsRepo = new Repository<DbItemContainerRelation>(db);

        await containersRepo.InsertAsync(new DbContainer { ContainerId = containerId, Name = "Container", Notes = "Notes" });
        await itemsRepo.InsertAsync(new DbItem { ItemId = item1Id, Name = "Item1", Description = "D1" });
        await itemsRepo.InsertAsync(new DbItem { ItemId = item2Id, Name = "Item2", Description = "D2" });

        await relationsRepo.InsertAsync(new DbItemContainerRelation { ContainerId = containerId, ItemId = item1Id, Quantity = 5 });
        await relationsRepo.InsertAsync(new DbItemContainerRelation { ContainerId = containerId, ItemId = item2Id, Quantity = 1 });

        await imagesRepo.InsertAsync(new DbImage { ImageId = keepImageId, OwnerUniqueId = containerId });
        await imagesRepo.InsertAsync(new DbImage { ImageId = extraImageId, OwnerUniqueId = containerId });

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "Container", Notes = "Notes" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = item1Id, Name = "Item1", Description = "D1" },
                    new InventoryBackupItem { ItemId = item2Id, Name = "Item2", Description = "D2" },
                ],
                Relations =
                [
                    // Lower than existing: FullSync should NOT reduce to 2.
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = item1Id, Quantity = 2 },
                ],
                Images =
                [
                    // Keep one image, omit one image: FullSync should NOT delete omitted one.
                    new InventoryBackupImageRef
                    {
                        ImageId = keepImageId,
                        OwnerId = containerId,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{keepImageId}.jpg",
                    },
                ],
            },
        });

        IInventoryBackupRestoreService service = new SqliteInventoryBackupRestoreService(db);
        var result = await service.RestoreAsync(backup, new InventoryBackupRestoreOptions
        {
            ConflictPolicy = InventoryBackupConflictPolicy.FullSync,
        });

        var remainingRelations = await relationsRepo.GetAllAsync();
        var remainingImages = await imagesRepo.GetAllAsync();

        var relationForItem1 = remainingRelations.Single(r => r.ItemId == item1Id && r.ContainerId == containerId);
        var relationForItem2 = remainingRelations.Single(r => r.ItemId == item2Id && r.ContainerId == containerId);

        Assert.Multiple(() =>
        {
            Assert.That(relationForItem1.Quantity, Is.EqualTo(5));
            Assert.That(relationForItem2.Quantity, Is.EqualTo(1));
            Assert.That(remainingImages.Select(i => i.ImageId), Is.EquivalentTo(new[] { keepImageId, extraImageId }));

            Assert.That(result.DeletedRelations, Is.EqualTo(0));
            Assert.That(result.DeletedImages, Is.EqualTo(0));
        });
    }
}
