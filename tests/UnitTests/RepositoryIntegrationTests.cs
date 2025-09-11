using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.Mappers;
using Infrastructure.Interfaces;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;

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
    private InventoryDomainRepository domainRepo = null!;

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
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<InventoryDomainRepository>();
        domainRepo = new InventoryDomainRepository(containers, items, photos, relations, logger);
    }

    [TearDown]
    public void Teardown()
    {
        try
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
        catch (IOException)
        {
            // Best effort cleanup in tests; ignore file locks
        }
    }

    [Test]
    public async Task Can_Insert_And_Load_Container_With_Photo()
    {
        var c = new Container(Guid.NewGuid(), "Test Container", "Notes");
        c.AddImageItem();
        await domainRepo.InsertContainerAsync(c);
        await domainRepo.InsertImageItemAsync(c.Photos[0], c.ContainerId);

        var loaded = await domainRepo.GetContainerAsync(c.ContainerId.ToString());
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Photos.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Can_Relate_Item_To_Container_And_Load_ItemsForContainer()
    {
        var c = new Container(Guid.NewGuid(), "C1", "");
        await domainRepo.InsertContainerAsync(c);
        var i = new Item { Name = "ItemA" };
        await domainRepo.InsertItemAsync(i);
        await domainRepo.InsertItemContainerRelation(i.ItemId, c.ContainerId);

        var itemsForContainer = await domainRepo.GetItemsForContainerAsync(c.ContainerId.ToString());
        Assert.That(itemsForContainer.Count, Is.EqualTo(1));
        Assert.That(itemsForContainer[0].Name, Is.EqualTo("ItemA"));
    }
}
