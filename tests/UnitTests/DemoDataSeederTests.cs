using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests;

[TestFixture]
public class DemoDataSeederTests
{
    private const string SeedMarker = "[SEED-CONTAINER-MARKER:4f3c5d11-2f9b-44b3-9e55-2e0f1ea7a8d2]";

    [Test]
    public async Task EnsureItemsAsync_SeedsOnlySeededContainers_LeavesUserContainersEmpty()
    {
        var seededContainerId = Guid.NewGuid();
        var userContainerId = Guid.NewGuid();

        var containersData = new List<DbContainer>
        {
            new()
            {
                ContainerId = seededContainerId,
                Name = "Container 1",
                Notes = $"Seeded notes for container abc12345 {SeedMarker}"
            },
            new()
            {
                ContainerId = userContainerId,
                Name = "User Container",
                Notes = "Seeded notes for container abc12345"
            }
        };

        var createdItems = new List<DbItem>();
        var createdInventories = new List<DbItemInventory>();
        var createdRelations = new List<DbItemContainerRelation>();

        var containersRepo = new Mock<IRepository<DbContainer>>();
        containersRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        containersRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(containersData);

        var itemsRepo = new Mock<IRepository<DbItem>>();
        itemsRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        itemsRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => createdItems.ToList());
        itemsRepo.Setup(r => r.InsertAsync(It.IsAny<DbItem>()))
            .Callback<DbItem>(createdItems.Add)
            .ReturnsAsync(1);

        var inventoriesRepo = new Mock<IRepository<DbItemInventory>>();
        inventoriesRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        inventoriesRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => createdInventories.ToList());
        inventoriesRepo.Setup(r => r.InsertAsync(It.IsAny<DbItemInventory>()))
            .Callback<DbItemInventory>(createdInventories.Add)
            .ReturnsAsync(1);

        var photosRepo = new Mock<IRepository<DbImage>>();
        photosRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        photosRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        photosRepo.Setup(r => r.InsertAsync(It.IsAny<DbImage>())).ReturnsAsync(1);

        var relationRepo = new Mock<IRepository<DbItemContainerRelation>>();
        relationRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        relationRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => createdRelations.ToList());
        relationRepo.Setup(r => r.WhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DbItemContainerRelation, bool>>>() ))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<DbItemContainerRelation, bool>> predicate) =>
                createdRelations.Where(predicate.Compile()).ToList());
        relationRepo.Setup(r => r.InsertAsync(It.IsAny<DbItemContainerRelation>()))
            .Callback<DbItemContainerRelation>(createdRelations.Add)
            .ReturnsAsync(1);

        var fileHandler = new Mock<IFileHandler>();

        var sut = new DemoDataSeeder(
            containersRepo.Object,
            itemsRepo.Object,
            inventoriesRepo.Object,
            photosRepo.Object,
            relationRepo.Object,
            fileHandler.Object,
            NullLogger<DemoDataSeeder>.Instance);

        await sut.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: false);

        Assert.That(createdRelations.Count, Is.EqualTo(3), "Expected exactly 3 seeded item relations.");
        Assert.That(createdRelations.All(r => r.ContainerId == seededContainerId), Is.True,
            "Containers without the seed GUID marker should not receive auto-seeded items.");
        Assert.That(createdInventories.All(inventory => inventory.TotalQuantity == 1), Is.True,
            "Each seeded item must include the quantity allocated to its seeded container.");
    }

    [Test]
    public async Task EnsureItemsAsync_WhenSeededItemWasUnassigned_DoesNotCreateDuplicateItemName()
    {
        var seededContainerId = Guid.NewGuid();
        var existingUnassignedItemId = Guid.NewGuid();
        var containersData = new List<DbContainer>
        {
            new()
            {
                ContainerId = seededContainerId,
                Name = "Container 2",
                Notes = $"Seeded notes for container abc12345 {SeedMarker}"
            },
        };
        var existingItems = new List<DbItem>
        {
            new() { ItemId = Guid.NewGuid(), Name = "Item Container 2-1" },
            new() { ItemId = Guid.NewGuid(), Name = "Item Container 2-2" },
            new() { ItemId = existingUnassignedItemId, Name = "Item Container 2-3" },
        };
        var createdItems = new List<DbItem>();
        var createdRelations = new List<DbItemContainerRelation>
        {
            new() { ItemId = existingItems[0].ItemId, ContainerId = seededContainerId, Quantity = 1 },
            new() { ItemId = existingItems[1].ItemId, ContainerId = seededContainerId, Quantity = 1 },
        };

        var containersRepo = new Mock<IRepository<DbContainer>>();
        containersRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        containersRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(containersData);

        var itemsRepo = new Mock<IRepository<DbItem>>();
        itemsRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        itemsRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => existingItems.Concat(createdItems).ToList());
        itemsRepo.Setup(r => r.InsertAsync(It.IsAny<DbItem>()))
            .Callback<DbItem>(createdItems.Add)
            .ReturnsAsync(1);

        var inventoriesRepo = new Mock<IRepository<DbItemInventory>>();
        inventoriesRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        inventoriesRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);
        inventoriesRepo.Setup(r => r.InsertAsync(It.IsAny<DbItemInventory>())).ReturnsAsync(1);

        var photosRepo = new Mock<IRepository<DbImage>>();
        photosRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        photosRepo.Setup(r => r.GetAllAsync()).ReturnsAsync([]);

        var relationRepo = new Mock<IRepository<DbItemContainerRelation>>();
        relationRepo.Setup(r => r.InitializeAsync()).Returns(Task.CompletedTask);
        relationRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(() => createdRelations.ToList());
        relationRepo.Setup(r => r.InsertAsync(It.IsAny<DbItemContainerRelation>()))
            .Callback<DbItemContainerRelation>(createdRelations.Add)
            .ReturnsAsync(1);

        var sut = new DemoDataSeeder(
            containersRepo.Object,
            itemsRepo.Object,
            inventoriesRepo.Object,
            photosRepo.Object,
            relationRepo.Object,
            Mock.Of<IFileHandler>(),
            NullLogger<DemoDataSeeder>.Instance);

        await sut.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: false);

        Assert.That(createdItems, Is.Empty);
    }
}
