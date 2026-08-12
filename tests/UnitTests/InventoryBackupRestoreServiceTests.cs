using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Specifications;
using Moq;

namespace UnitTests;

[TestFixture]
public class InventoryBackupRestoreServiceTests
{
    [Test]
    public async Task RestoreAsync_AddsOnlyMissingParts_WhenDatabaseAlreadyHasData()
    {
        var container1Id = Guid.NewGuid();
        var container2Id = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var existingContainerPhoto = Guid.NewGuid();
        var existingItemPhoto = Guid.NewGuid();
        var newContainerPhoto = Guid.NewGuid();
        var newItemPhoto = Guid.NewGuid();

        var existingContainer = new Container(container1Id, "Existing container", "notes");
        existingContainer.AddItem(item1Id, 2);
        existingContainer.AddImageItem(existingContainerPhoto);

        var existingItem = new Item
        {
            ItemId = item1Id,
            Name = "Existing item",
            Description = "desc",
            Photos = [new ImageItem(existingItemPhoto)],
        };

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = container1Id, Name = "Existing container", Notes = "notes" },
                    new InventoryBackupContainer { ContainerId = container2Id, Name = "New container", Notes = "new" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = item1Id, Name = "Existing item", Description = "desc" },
                    new InventoryBackupItem { ItemId = item2Id, Name = "New item", Description = "new" },
                ],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = container1Id, ItemId = item1Id, Quantity = 5 },
                    new InventoryBackupRelation { ContainerId = container2Id, ItemId = item2Id, Quantity = 1 },
                ],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        ImageId = existingContainerPhoto,
                        OwnerId = container1Id,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{existingContainerPhoto}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = newContainerPhoto,
                        OwnerId = container1Id,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{newContainerPhoto}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = existingItemPhoto,
                        OwnerId = item1Id,
                        OwnerType = InventoryBackupOwnerType.Item,
                        FileName = $"{existingItemPhoto}.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        ImageId = newItemPhoto,
                        OwnerId = item2Id,
                        OwnerType = InventoryBackupOwnerType.Item,
                        FileName = $"{newItemPhoto}.jpg",
                    },
                ],
            },
        };

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([existingContainer]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([existingItem]);

        var commands = new Mock<IInventoryCommandRepository>();

        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);
        var result = await sut.RestoreAsync(backup);

        commands.Verify(c => c.InsertContainerAsync(It.Is<Container>(x => x.ContainerId == container2Id)), Times.Once);
        commands.Verify(c => c.InsertItemAsync(It.Is<Item>(x => x.ItemId == item2Id)), Times.Once);

        commands.Verify(c => c.InsertItemContainerRelation(item1Id, container1Id, 3), Times.Once);
        commands.Verify(c => c.InsertItemContainerRelation(item2Id, container2Id, 1), Times.Once);

        commands.Verify(c => c.InsertImageItemAsync(It.Is<ImageItem>(i => i.ImageId == newContainerPhoto), container1Id), Times.Once);
        commands.Verify(c => c.InsertImageItemAsync(It.Is<ImageItem>(i => i.ImageId == newItemPhoto), item2Id), Times.Once);

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
    public void RestoreFromJsonAsync_ThrowsArgumentException_ForInvalidJson()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync("{invalid-json"));
    }
}
