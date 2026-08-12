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
        catch
        {
            // ignore disposal issues in tests
        }

        try
        {
            if (!string.IsNullOrEmpty(dbPath) && File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
        catch
        {
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
}
