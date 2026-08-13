using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Infrastructure.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using Infrastructure.Services.Repositories;

namespace UnitTests;

[TestFixture]
public class RepositoryIntegrationTests
{
    private string dbPath = null!;
    private MothballDatabase db = null!;
    private IRepository<DbContainer> containers = null!;
    private IRepository<DbItem> items = null!;
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
        photos = new Repository<DbImage>(db);
        relations = new Repository<DbItemContainerRelation>(db);
        await db.InitializeAsync();

        var containerLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContainerRepository>();
        var itemLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ItemRepository>();
        var transactionRunner = new SqliteTransactionRunner(db);

        var containerRepo = new ContainerRepository(transactionRunner, containers, photos, relations, containerLogger);
        var itemRepo = new ItemRepository(transactionRunner, items, photos, relations, itemLogger);
        var imageRepo = new ImageRepository(photos);
        var relationRepo = new RelationRepository(relations);

        queryRepo = new InventoryQueryRepository(containerRepo, itemRepo);
        commandRepo = new InventoryCommandRepository(containerRepo, itemRepo, imageRepo, relationRepo);
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
        await commandRepo.InsertItemContainerRelation(i.ItemId, c.ContainerId, quantity: 2);

        var itemsForContainer = await queryRepo.GetItemsForContainerAsync(c.ContainerId.ToString());
        Assert.That(itemsForContainer.Count, Is.EqualTo(1));
        Assert.That(itemsForContainer[0].Name, Is.EqualTo("ItemA"));
        Assert.That(itemsForContainer[0].Description, Is.EqualTo("DescA"));
    }
}
