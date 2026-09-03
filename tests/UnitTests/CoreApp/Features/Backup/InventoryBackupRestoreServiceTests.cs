using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Specifications;
using Moq;
using System.Text.Json;

namespace Mothball.Tests.Unit.Core.Features.Backup;

[TestFixture]
public class InventoryBackupRestoreServiceTests
{
    [Test]
    public async Task RestoreAsync_InsertsBarcodeMetadata()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer
                    {
                        ContainerId = containerId,
                        Name = "Box",
                        BarcodeValue = "CONTAINER-42",
                        BarcodeSymbology = (int)BarcodeSymbology.Code128,
                    },
                ],
                Items =
                [
                    new InventoryBackupItem
                    {
                        ItemId = itemId,
                        Name = "Cable",
                        BarcodeValue = "1234567890123",
                        BarcodeSymbology = (int)BarcodeSymbology.Ean13,
                    },
                ],
            },
        });
        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(query => query.QueryContainersAsync(It.IsAny<ContainerListSpecification>())).ReturnsAsync([]);
        queries.Setup(query => query.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>())).ReturnsAsync([]);
        queries.Setup(query => query.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>())).ReturnsAsync([]);
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        await sut.RestoreAsync(backup);

        commands.Verify(command => command.InsertContainerAsync(It.Is<Container>(container =>
            container.ContainerId == containerId
            && container.Barcode == new Barcode("CONTAINER-42", BarcodeSymbology.Code128))), Times.Once);
        commands.Verify(command => command.InsertItemAsync(It.Is<Item>(item =>
            item.ItemId == itemId
            && item.Barcode == new Barcode("1234567890123", BarcodeSymbology.Ean13))), Times.Once);
    }

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
        existingContainer.AddImageItem(existingContainerPhoto);

        var existingItem = new Item(item1Id, "Existing item", "desc");
        existingItem.AddImageItem(existingItemPhoto);

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
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
        });

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([existingContainer]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([existingItem]);
        queries.Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync(
            [
                new InventorySnapshot(
                    existingItem,
                    totalQuantity: 5,
                    assignedQuantity: 2,
                    allocations:
                    [
                        new ItemContainerAllocation(container1Id, existingContainer.Name, 2),
                    ]),
            ]);

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

    [Test]
    public void RestoreFromJsonAsync_ThrowsArgumentException_WhenDataPropertyIsMissing()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var json = """
            {
                "payloadVersion": 1,
                "schemaVersion": 1,
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "source": "MothballMobile",
                "integrity": {
                    "checksumAlgorithm": "SHA256",
                    "payloadChecksum": ""
                }
            }
            """;

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync(
            json,
            new InventoryBackupRestoreOptions { RequireIntegrityValidation = false }));
    }

    [Test]
    public void RestoreFromJsonAsync_ThrowsArgumentException_WhenCreatedUtcPropertyIsMissing()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var json = """
            {
                "payloadVersion": 1,
                "schemaVersion": 1,
                "source": "MothballMobile",
                "integrity": {
                    "checksumAlgorithm": "SHA256",
                    "payloadChecksum": ""
                },
                "data": {
                    "containers": [],
                    "items": [],
                    "relations": [],
                    "images": []
                }
            }
            """;

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync(
            json,
            new InventoryBackupRestoreOptions { RequireIntegrityValidation = false }));
    }

    [Test]
    public void RestoreFromJsonAsync_ThrowsArgumentException_WhenCreatedUtcIsDefault()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            CreatedUtc = default,
            Data = new InventoryBackupData(),
        });

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync(
            json,
            new InventoryBackupRestoreOptions { RequireIntegrityValidation = false }));
    }

    [Test]
    public void RestoreFromJsonAsync_ThrowsArgumentException_WhenIntegrityChecksumAlgorithmIsMissing()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var json = """
            {
                "payloadVersion": 1,
                "schemaVersion": 1,
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "source": "MothballMobile",
                "integrity": {
                    "payloadChecksum": ""
                },
                "data": {
                    "containers": [],
                    "items": [],
                    "relations": [],
                    "images": []
                }
            }
            """;

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync(
            json,
            new InventoryBackupRestoreOptions { RequireIntegrityValidation = false }));
    }

    [Test]
    public void RestoreFromJsonAsync_ThrowsArgumentException_WhenDataImagesPropertyIsMissing()
    {
        var queries = new Mock<IInventoryQueryRepository>();
        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var json = """
            {
                "payloadVersion": 1,
                "schemaVersion": 1,
                "createdUtc": "2026-01-01T00:00:00+00:00",
                "source": "MothballMobile",
                "integrity": {
                    "checksumAlgorithm": "SHA256",
                    "payloadChecksum": ""
                },
                "data": {
                    "containers": [],
                    "items": [],
                    "relations": []
                }
            }
            """;

        Assert.ThrowsAsync<ArgumentException>(() => sut.RestoreFromJsonAsync(
            json,
            new InventoryBackupRestoreOptions { RequireIntegrityValidation = false }));
    }

    [Test]
    public async Task RestoreFromJsonAsync_WithValidJson_ParsesAndRestores()
    {
        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            CreatedUtc = DateTimeOffset.UtcNow,
            Data = new InventoryBackupData
            {
                Containers = [],
                Items = [],
                Relations = [],
                Images = [],
            },
        });

        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([]);

        var commands = new Mock<IInventoryCommandRepository>();

        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);
        var result = await sut.RestoreFromJsonAsync(json);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.AddedContainers, Is.EqualTo(0));
        Assert.That(result.AddedItems, Is.EqualTo(0));

        queries.Verify(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()), Times.Once);
        queries.Verify(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()), Times.Once);
    }

    [Test]
    public async Task RestoreAsync_AddAndUpsertMetadata_UpdatesExistingRows()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var existingContainer = new Container(containerId, "Old", "Old Notes");
        var existingItem = new Item(itemId, "Old Item", "Old Description");

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "New", Notes = "New Notes" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = itemId, Name = "New Item", Description = "New Description" },
                ],
                Relations = [],
                Images = [],
            },
        });

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([existingContainer]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([existingItem]);

        var commands = new Mock<IInventoryCommandRepository>();

        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);
        var result = await sut.RestoreAsync(backup, new InventoryBackupRestoreOptions
        {
            ConflictPolicy = InventoryBackupConflictPolicy.AddAndUpsertMetadata,
        });

        commands.Verify(c => c.UpdateContainerAsync(It.Is<Container>(x => x.ContainerId == containerId && x.Name == "New" && x.Notes == "New Notes")), Times.Once);
        commands.Verify(c => c.UpdateItemAsync(It.Is<Item>(x => x.ItemId == itemId && x.Name == "New Item" && x.Description == "New Description")), Times.Once);

        Assert.That(result.UpdatedContainers, Is.EqualTo(1));
        Assert.That(result.UpdatedItems, Is.EqualTo(1));
    }

    [Test]
    public async Task RestoreAsync_FullSync_DeletesMissingRootEntities()
    {
        var existingContainer = new Container(Guid.NewGuid(), "Existing", "Notes");
        var existingItem = new Item("Existing Item", "Description");

        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [],
                Items = [],
                Relations = [],
                Images = [],
            },
        });

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([existingContainer]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([existingItem]);

        var commands = new Mock<IInventoryCommandRepository>();

        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);
        var result = await sut.RestoreAsync(backup, new InventoryBackupRestoreOptions
        {
            ConflictPolicy = InventoryBackupConflictPolicy.FullSync,
        });

        commands.Verify(c => c.DeleteItemAsync(existingItem.ItemId.ToString()), Times.Once);
        commands.Verify(c => c.DeleteContainerAsync(existingContainer.ContainerId.ToString()), Times.Once);

        Assert.That(result.DeletedContainers, Is.EqualTo(1));
        Assert.That(result.DeletedItems, Is.EqualTo(1));
    }

    [Test]
    public void RestoreAsync_ThrowsInvalidDataException_WhenChecksumDoesNotMatch()
    {
        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [],
                Items = [],
                Relations = [],
                Images = [],
            },
        });

        var tamperedBackup = backup with
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = Guid.NewGuid(), Name = "Tampered", Notes = "Tampered" }],
                Items = [],
                Relations = [],
                Images = [],
            },
        };

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync(new List<Container>());
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync(new List<Item>());

        var commands = new Mock<IInventoryCommandRepository>();

        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        Assert.ThrowsAsync<InvalidDataException>(() => sut.RestoreAsync(tamperedBackup));
    }

    [Test]
    public async Task RestoreAsync_StrictFullSync_ReconcilesRelationsAndImagesExactly()
    {
        var containerId = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var existingContainer = new Container(containerId, "Container", "Notes");

        var keepImageId = Guid.NewGuid();
        var deleteImageId = Guid.NewGuid();
        existingContainer.AddImageItem(keepImageId);
        existingContainer.AddImageItem(deleteImageId);

        var existingItem1 = new Item(item1Id, "Item 1", "D1");

        var existingItem2 = new Item(item2Id, "Item 2", "D2");

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
                    new InventoryBackupItem { ItemId = item1Id, Name = "Item 1", Description = "D1" },
                    new InventoryBackupItem { ItemId = item2Id, Name = "Item 2", Description = "D2" },
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

        var queries = new Mock<IInventoryQueryRepository>();
        queries.Setup(q => q.QueryContainersAsync(It.IsAny<ContainerListSpecification>()))
            .ReturnsAsync([existingContainer]);
        queries.Setup(q => q.QueryItemsWithPhotosAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync([existingItem1, existingItem2]);
        queries.Setup(q => q.QueryInventorySnapshotsAsync(It.IsAny<ItemListSpecification>()))
            .ReturnsAsync(
            [
                new InventorySnapshot(
                    existingItem1,
                    totalQuantity: 5,
                    assignedQuantity: 5,
                    allocations:
                    [
                        new ItemContainerAllocation(containerId, existingContainer.Name, 5),
                    ]),
                new InventorySnapshot(
                    existingItem2,
                    totalQuantity: 1,
                    assignedQuantity: 1,
                    allocations:
                    [
                        new ItemContainerAllocation(containerId, existingContainer.Name, 1),
                    ]),
            ]);

        var commands = new Mock<IInventoryCommandRepository>();
        var sut = new InventoryBackupRestoreService(queries.Object, commands.Object);

        var result = await sut.RestoreAsync(backup, new InventoryBackupRestoreOptions
        {
            ConflictPolicy = InventoryBackupConflictPolicy.StrictFullSync,
        });

        commands.Verify(c => c.ReplaceItemContainerRelationQuantity(item1Id, containerId, 2), Times.Once);
        commands.Verify(c => c.DeleteItemContainerRelation(item2Id, containerId), Times.Once);
        commands.Verify(c => c.DeleteImageItemAsync(deleteImageId, containerId), Times.Once);

        Assert.Multiple(() =>
        {
            Assert.That(result.DeletedItems, Is.EqualTo(0));
            Assert.That(result.DeletedRelations, Is.EqualTo(1));
            Assert.That(result.DeletedImages, Is.EqualTo(1));
        });
    }
}
