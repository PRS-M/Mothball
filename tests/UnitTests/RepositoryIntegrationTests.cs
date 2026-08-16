using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Infrastructure.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Inventory;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Specifications;
using Infrastructure.Services.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

[TestFixture]
public class RepositoryIntegrationTests
{
    private string dbPath = null!;
    private MothballDatabase db = null!;
    private IRepository<DbContainer> containers = null!;
    private IRepository<DbItem> items = null!;
    private IRepository<DbItemInventory> inventories = null!;
    private IRepository<DbImage> photos = null!;
    private IRepository<DbItemContainerRelation> relations = null!;
    private IInventoryQueryRepository queryRepo = null!;
    private IInventoryCommandRepository commandRepo = null!;

    [SetUp]
    public async Task Setup()
    {
        dbPath = Path.Combine(Path.GetTempPath(), $"mothball-test-{Guid.NewGuid():N}.db");
        db = new MothballDatabase(dbPath);
        containers = new Repository<DbContainer>(db);
        items = new Repository<DbItem>(db);
        inventories = new Repository<DbItemInventory>(db);
        photos = new Repository<DbImage>(db);
        relations = new Repository<DbItemContainerRelation>(db);
        await db.InitializeAsync();

        var containerLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContainerRepository>();
        var itemLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ItemRepository>();
        var transactionRunner = new SqliteTransactionRunner(db);

        var containerRepo = new ContainerRepository(transactionRunner, containers, photos, relations, containerLogger);
        var itemRepo = new ItemRepository(transactionRunner, items, photos, relations, itemLogger);
        var itemInventoryRepo = new ItemInventoryRepository(inventories, relations, containers, transactionRunner);
        var imageRepo = new ImageRepository(photos);
        var relationRepo = new RelationRepository(relations, transactionRunner);

        queryRepo = new InventoryQueryRepository(containerRepo, itemRepo, itemInventoryRepo);
        commandRepo = new InventoryCommandRepository(containerRepo, itemRepo, itemInventoryRepo, imageRepo, relationRepo);
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
        catch (IOException ex)
        {
            TestContext.Error.WriteLine($"Failed to delete test database '{dbPath}': {ex}");
            // Best effort cleanup in tests; ignore file locks
        }
    }

    [Test]
    public async Task Can_Insert_And_Load_Container_With_Photo()
    {
        var c = new Container(Guid.NewGuid(), "Test Container", "Notes");
        c.AddImageItem();
        await commandRepo.InsertContainerAsync(c);
        await commandRepo.InsertImageItemAsync(c.Photos[0], c.ContainerId);

        var loaded = await queryRepo.GetContainerAsync(c.ContainerId.ToString());
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Photos.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_Relate_Item_To_Container_And_Load_ItemsForContainer()
    {
        var c = new Container(Guid.NewGuid(), "C1", "");
        await commandRepo.InsertContainerAsync(c);
        var i = new Item("ItemA", "DescA");
        await commandRepo.InsertItemAsync(i);
        await commandRepo.InsertItemInventoryAsync(new ItemInventory(
            i.ItemId,
            2,
            [new ItemContainerAllocation(c.ContainerId, c.Name, 2)]));

        var itemsForContainer = await queryRepo.QueryContainerItemsWithPhotosAsync(
            new ContainerItemsSpecification(c.ContainerId.ToString()));
        Assert.That(itemsForContainer.Count, Is.EqualTo(1));
        Assert.That(itemsForContainer[0].Name, Is.EqualTo("ItemA"));
        Assert.That(itemsForContainer[0].Description, Is.EqualTo("DescA"));
    }

    [Test]
    public async Task InsertItemContainerRelation_WhenCalledTwice_StoresSingleRelationRow()
    {
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        await commandRepo.InsertContainerAsync(container);
        await commandRepo.InsertItemAsync(item);
        await commandRepo.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 5));

        await commandRepo.InsertItemContainerRelation(item.ItemId, container.ContainerId, 2);
        await commandRepo.InsertItemContainerRelation(item.ItemId, container.ContainerId, 3);

        var relationRows = await relations.WhereAsync(relation =>
            relation.ItemId == item.ItemId && relation.ContainerId == container.ContainerId);

        Assert.Multiple(() =>
        {
            Assert.That(relationRows, Has.Count.EqualTo(1));
            Assert.That(relationRows.Single().Quantity, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task EditUnassignAndReassignSameContainer_StoresSingleRelationRow()
    {
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        await commandRepo.InsertContainerAsync(container);
        await commandRepo.InsertItemAsync(item);
        await commandRepo.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 5));

        var inventoryCommands = new ItemInventoryCommandService(queryRepo, commandRepo);
        var quantityService = new ContainerItemQuantityService(inventoryCommands);
        var assignHandler = new AssignItemToContainerCommandHandler(inventoryCommands);
        var currentContainer = (await queryRepo.GetContainerAsync(container.ContainerId.ToString()))!;

        await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 4);
        await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 0);
        await assignHandler.AssignAsync(item.ItemId, container.ContainerId, 1);

        var relationRows = await relations.WhereAsync(relation =>
            relation.ItemId == item.ItemId && relation.ContainerId == container.ContainerId);

        Assert.Multiple(() =>
        {
            Assert.That(relationRows, Has.Count.EqualTo(1));
            Assert.That(relationRows.Single().Quantity, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DemoSeeder_RemovesDuplicateSeededItemName_KeepingHighestTotalQuantity()
    {
        var container = new DbContainer
        {
            ContainerId = Guid.NewGuid(),
            Name = "Container 2",
            Notes = "Seeded notes for container abc12345 [SEED-CONTAINER-MARKER:4f3c5d11-2f9b-44b3-9e55-2e0f1ea7a8d2]",
        };
        var originalItem = new DbItem
        {
            ItemId = Guid.NewGuid(),
            Name = "Item Container 2-3",
        };
        var duplicateItem = new DbItem
        {
            ItemId = Guid.NewGuid(),
            Name = "Item Container 2-3",
        };

        await containers.InsertAsync(container);
        await items.InsertAsync(originalItem);
        await items.InsertAsync(duplicateItem);
        await inventories.InsertAsync(new DbItemInventory { ItemId = originalItem.ItemId, TotalQuantity = 10 });
        await inventories.InsertAsync(new DbItemInventory { ItemId = duplicateItem.ItemId, TotalQuantity = 1 });
        await relations.InsertAsync(new DbItemContainerRelation
        {
            ItemId = originalItem.ItemId,
            ContainerId = container.ContainerId,
            Quantity = 10,
        });
        await relations.InsertAsync(new DbItemContainerRelation
        {
            ItemId = duplicateItem.ItemId,
            ContainerId = container.ContainerId,
            Quantity = 1,
        });

        var seeder = new DemoDataSeeder(
            containers,
            items,
            inventories,
            photos,
            relations,
            Mock.Of<IFileHandler>(),
            NullLogger<DemoDataSeeder>.Instance);

        await seeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: false);

        var remainingItems = (await items.GetAllAsync())
            .Where(item => item.Name == "Item Container 2-3")
            .ToList();
        var remainingRelations = await relations.WhereAsync(relation =>
            relation.ItemId == duplicateItem.ItemId || relation.ItemId == originalItem.ItemId);
        var remainingInventories = await inventories.GetAllAsync();

        Assert.Multiple(() =>
        {
            Assert.That(remainingItems, Has.Count.EqualTo(1));
            Assert.That(remainingItems.Single().ItemId, Is.EqualTo(originalItem.ItemId));
            Assert.That(remainingRelations.Select(relation => relation.ItemId), Is.EqualTo(new[] { originalItem.ItemId }));
            Assert.That(remainingInventories.Any(inventory => inventory.ItemId == duplicateItem.ItemId), Is.False);
        });
    }
}
