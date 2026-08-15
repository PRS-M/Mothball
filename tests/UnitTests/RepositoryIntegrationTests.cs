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
}
