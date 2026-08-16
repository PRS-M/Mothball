using CoreApp.Entities.Inventory;
using System.Text.Json;
using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Specifications;
using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Infrastructure.Services.Repositories;
using Moq;

namespace UnitTests;

[TestFixture]
public class BackendParityTests
{
    [Test]
    public async Task QueryContainersAsync_OrdersAllResultsByInsertionAndPagesConsistently()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var id in ids)
        {
            await sqlite.Command.InsertContainerAsync(new Container(id, $"Container {id:N}", ""));
            await json.Command.InsertContainerAsync(new Container(id, $"Container {id:N}", ""));
        }

        var specification = new ContainerListSpecification(
            ContainerQueryFilter.All,
            PageNumber: 1,
            PageSize: 1);

        var sqliteContainers = await sqlite.Query.QueryContainersAsync(specification);
        var jsonContainers = await json.Query.QueryContainersAsync(specification);

        Assert.That(sqliteContainers.Select(c => c.ContainerId), Is.EqualTo(new[] { ids[1] }));
        Assert.That(jsonContainers.Select(c => c.ContainerId), Is.EqualTo(new[] { ids[1] }));
    }

    [Test]
    public async Task QueryContainersAsync_EmptySearchOrdersByNameCaseInsensitively()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var assigned = new Container(Guid.NewGuid(), "Assigned", "");
        var beta = new Container(Guid.NewGuid(), "beta", "storage shelf");
        var alpha = new Container(Guid.NewGuid(), "Alpha", "storage shelf");
        var item = new Item(Guid.NewGuid(), "Item", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(assigned);
            await command.InsertContainerAsync(beta);
            await command.InsertContainerAsync(alpha);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, assigned.ContainerId, 1);
        }

        var specification = new ContainerListSpecification(
            ContainerQueryFilter.Empty,
            SearchTerm: "storage");

        var sqliteContainers = await sqlite.Query.QueryContainersAsync(specification);
        var jsonContainers = await json.Query.QueryContainersAsync(specification);

        Assert.That(sqliteContainers.Select(c => c.Name), Is.EqualTo(new[] { "Alpha", "beta" }));
        Assert.That(jsonContainers.Select(c => c.Name), Is.EqualTo(new[] { "Alpha", "beta" }));
    }

    [Test]
    public async Task QueryItemsWithPhotosAsync_SearchKeepsInsertionOrderAndPhotoOwnership()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var firstItem = new Item(Guid.NewGuid(), "Cable", "USB-C");
        var secondItem = new Item(Guid.NewGuid(), "cable tie", "Velcro");
        var firstPhotoId = Guid.NewGuid();
        var secondPhotoId = Guid.NewGuid();

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertItemAsync(firstItem);
            await command.InsertItemAsync(secondItem);
            await command.InsertImageItemAsync(new ImageItem(firstPhotoId), firstItem.ItemId);
            await command.InsertImageItemAsync(new ImageItem(secondPhotoId), secondItem.ItemId);
        }

        var specification = new ItemListSpecification(
            ItemQueryFilter.All,
            SearchTerm: "CABLE");

        var sqliteItems = await sqlite.Query.QueryItemsWithPhotosAsync(specification);
        var jsonItems = await json.Query.QueryItemsWithPhotosAsync(specification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItems.Select(i => i.ItemId), Is.EqualTo(new[] { firstItem.ItemId, secondItem.ItemId }));
            Assert.That(jsonItems.Select(i => i.ItemId), Is.EqualTo(new[] { firstItem.ItemId, secondItem.ItemId }));
            Assert.That(sqliteItems.SelectMany(i => i.Photos.Select(p => p.ImageId)), Is.EqualTo(new[] { firstPhotoId, secondPhotoId }));
            Assert.That(jsonItems.SelectMany(i => i.Photos.Select(p => p.ImageId)), Is.EqualTo(new[] { firstPhotoId, secondPhotoId }));
        });
    }

    [Test]
    public async Task QueryItemsWithPhotosAsync_SearchWithPaging_ReturnsRequestedPageAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var items = new[]
        {
            new Item(Guid.NewGuid(), "Cable A", ""),
            new Item(Guid.NewGuid(), "Cable B", ""),
            new Item(Guid.NewGuid(), "Cable C", ""),
        };

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            foreach (var item in items)
            {
                await command.InsertItemAsync(item);
            }
        }

        var specification = new ItemListSpecification(
            ItemQueryFilter.All,
            SearchTerm: "cable",
            PageNumber: 1,
            PageSize: 1);

        var sqliteItems = await sqlite.Query.QueryItemsWithPhotosAsync(specification);
        var jsonItems = await json.Query.QueryItemsWithPhotosAsync(specification);

        Assert.That(sqliteItems.Select(i => i.ItemId), Is.EqualTo(new[] { items[1].ItemId }));
        Assert.That(jsonItems.Select(i => i.ItemId), Is.EqualTo(new[] { items[1].ItemId }));
    }

    [Test]
    public async Task QueryInventorySnapshotsAsync_UnassignedSearchWithPaging_ReturnsRequestedPageAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var assignedOnly = new Item(Guid.NewGuid(), "Cable Assigned", "");
        var firstUnassigned = new Item(Guid.NewGuid(), "Cable Alpha", "");
        var secondUnassigned = new Item(Guid.NewGuid(), "Cable Beta", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(assignedOnly);
            await command.InsertItemAsync(firstUnassigned);
            await command.InsertItemAsync(secondUnassigned);
            await command.InsertItemInventoryAsync(new ItemInventory(assignedOnly.ItemId, 1));
            await command.InsertItemInventoryAsync(new ItemInventory(firstUnassigned.ItemId, 3));
            await command.InsertItemInventoryAsync(new ItemInventory(secondUnassigned.ItemId, 3));
            await command.InsertItemContainerRelation(assignedOnly.ItemId, container.ContainerId, 1);
            await command.InsertItemContainerRelation(firstUnassigned.ItemId, container.ContainerId, 2);
        }

        var specification = new ItemListSpecification(
            ItemQueryFilter.Unassigned,
            SearchTerm: "cable",
            PageNumber: 1,
            PageSize: 1);

        var sqliteItems = await sqlite.Query.QueryInventorySnapshotsAsync(specification);
        var jsonItems = await json.Query.QueryInventorySnapshotsAsync(specification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { secondUnassigned.ItemId }));
            Assert.That(jsonItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { secondUnassigned.ItemId }));
            Assert.That(sqliteItems.Single().UnassignedQuantity, Is.EqualTo(3));
            Assert.That(jsonItems.Single().UnassignedQuantity, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task QueryInventorySnapshotsAsync_AssignedSearchWithPaging_ReturnsRequestedPageAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var firstAssigned = new Item(Guid.NewGuid(), "Cable Alpha", "");
        var secondAssigned = new Item(Guid.NewGuid(), "Cable Beta", "");
        var unassignedOnly = new Item(Guid.NewGuid(), "Cable Loose", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(firstAssigned);
            await command.InsertItemAsync(secondAssigned);
            await command.InsertItemAsync(unassignedOnly);
            await command.InsertItemInventoryAsync(new ItemInventory(firstAssigned.ItemId, 1));
            await command.InsertItemInventoryAsync(new ItemInventory(secondAssigned.ItemId, 3));
            await command.InsertItemInventoryAsync(new ItemInventory(unassignedOnly.ItemId, 3));
            await command.InsertItemContainerRelation(firstAssigned.ItemId, container.ContainerId, 1);
            await command.InsertItemContainerRelation(secondAssigned.ItemId, container.ContainerId, 2);
        }

        var specification = new ItemListSpecification(
            ItemQueryFilter.Assigned,
            SearchTerm: "cable",
            PageNumber: 1,
            PageSize: 1);

        var sqliteItems = await sqlite.Query.QueryInventorySnapshotsAsync(specification);
        var jsonItems = await json.Query.QueryInventorySnapshotsAsync(specification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { secondAssigned.ItemId }));
            Assert.That(jsonItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { secondAssigned.ItemId }));
            Assert.That(sqliteItems.Single().AssignedQuantity, Is.EqualTo(2));
            Assert.That(jsonItems.Single().AssignedQuantity, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task QueryInventorySnapshotsAsync_UnassignedWithExcludedContainer_FiltersBeforePagingAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var targetContainer = new Container(Guid.NewGuid(), "Target", "");
        var otherContainer = new Container(Guid.NewGuid(), "Other", "");
        var alreadyInTarget = new Item(Guid.NewGuid(), "Cable Alpha", "");
        var availableFromOther = new Item(Guid.NewGuid(), "Cable Beta", "");
        var fullyUnassigned = new Item(Guid.NewGuid(), "Cable Gamma", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(targetContainer);
            await command.InsertContainerAsync(otherContainer);
            await command.InsertItemAsync(alreadyInTarget);
            await command.InsertItemAsync(availableFromOther);
            await command.InsertItemAsync(fullyUnassigned);
            await command.InsertItemInventoryAsync(new ItemInventory(alreadyInTarget.ItemId, 3));
            await command.InsertItemInventoryAsync(new ItemInventory(availableFromOther.ItemId, 3));
            await command.InsertItemInventoryAsync(new ItemInventory(fullyUnassigned.ItemId, 3));
            await command.InsertItemContainerRelation(alreadyInTarget.ItemId, targetContainer.ContainerId, 1);
            await command.InsertItemContainerRelation(availableFromOther.ItemId, otherContainer.ContainerId, 1);
        }

        var specification = new ItemListSpecification(
            ItemQueryFilter.Unassigned,
            PageNumber: 0,
            PageSize: 1,
            ExcludedContainerId: targetContainer.ContainerId);

        var sqliteItems = await sqlite.Query.QueryInventorySnapshotsAsync(specification);
        var jsonItems = await json.Query.QueryInventorySnapshotsAsync(specification);

        Assert.That(sqliteItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { availableFromOther.ItemId }));
        Assert.That(jsonItems.Select(i => i.Item.ItemId), Is.EqualTo(new[] { availableFromOther.ItemId }));
    }

    [Test]
    public async Task GetContainerAsync_AggregatesDuplicateRelationQuantitiesAndOwnsPhotos()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");
        var photoId = Guid.NewGuid();

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 2);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 3);
            await command.InsertImageItemAsync(new ImageItem(photoId), container.ContainerId);
        }

        var sqliteContainer = await sqlite.Query.GetContainerAsync(container.ContainerId.ToString());
        var jsonContainer = await json.Query.GetContainerAsync(container.ContainerId.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(sqliteContainer, Is.Not.Null);
            Assert.That(jsonContainer, Is.Not.Null);
            Assert.That(sqliteContainer!.Items.Single().Quantity, Is.EqualTo(5));
            Assert.That(jsonContainer!.Items.Single().Quantity, Is.EqualTo(5));
            Assert.That(sqliteContainer.Photos.Select(p => p.ImageId), Is.EqualTo(new[] { photoId }));
            Assert.That(jsonContainer.Photos.Select(p => p.ImageId), Is.EqualTo(new[] { photoId }));
        });
    }

    [Test]
    public async Task ItemInventoryTotalQuantity_PersistsAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var item = new Item(Guid.NewGuid(), "Widget", "");

        await sqlite.Command.InsertItemAsync(item);
        await json.Command.InsertItemAsync(item);
        await sqlite.Command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 12));
        await json.Command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 12));

        var sqliteItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(12));
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(12));
        });
    }

    [Test]
    public async Task InventorySnapshot_AggregatesAllocationsAcrossContainers()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var firstContainer = new Container(Guid.NewGuid(), "Box", "");
        var secondContainer = new Container(Guid.NewGuid(), "Drawer", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(firstContainer);
            await command.InsertContainerAsync(secondContainer);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 12));
            await command.InsertItemContainerRelation(item.ItemId, firstContainer.ContainerId, 3);
            await command.InsertItemContainerRelation(item.ItemId, secondContainer.ContainerId, 4);
        }

        var sqliteListItem = (await sqlite.Query.QueryInventorySnapshotsAsync(new ItemListSpecification(ItemQueryFilter.All))).Single();
        var jsonListItem = (await json.Query.QueryInventorySnapshotsAsync(new ItemListSpecification(ItemQueryFilter.All))).Single();
        var sqliteDetailsItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonDetailsItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            AssertInventorySnapshot(sqliteListItem);
            AssertInventorySnapshot(jsonListItem);
            AssertInventorySnapshot(sqliteDetailsItem!);
            AssertInventorySnapshot(jsonDetailsItem!);
        });
    }

    [Test]
    public async Task InventoryProjection_IsConsistentAcrossListDetailsAndContainerRows()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var box = new Container(Guid.NewGuid(), "Box", "");
        var drawer = new Container(Guid.NewGuid(), "Drawer", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(box);
            await command.InsertContainerAsync(drawer);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 10));
            await command.InsertItemContainerRelation(item.ItemId, box.ContainerId, 4);
            await command.InsertItemContainerRelation(item.ItemId, drawer.ContainerId, 3);
        }

        var sqliteSummaries = new[]
        {
            (await sqlite.Query.QueryInventorySnapshotsAsync(
                new ItemListSpecification(ItemQueryFilter.All))).Single(),
            (await sqlite.Query.GetInventorySnapshotAsync(item.ItemId))!,
            (await sqlite.Query.QueryContainerItemInventoryAsync(
                new ContainerItemsSpecification(box.ContainerId.ToString()))).Single().Inventory,
        };
        var jsonSummaries = new[]
        {
            (await json.Query.QueryInventorySnapshotsAsync(
                new ItemListSpecification(ItemQueryFilter.All))).Single(),
            (await json.Query.GetInventorySnapshotAsync(item.ItemId))!,
            (await json.Query.QueryContainerItemInventoryAsync(
                new ContainerItemsSpecification(box.ContainerId.ToString()))).Single().Inventory,
        };

        Assert.Multiple(() =>
        {
            foreach (var summary in sqliteSummaries.Concat(jsonSummaries))
            {
                Assert.That(summary.TotalQuantity, Is.EqualTo(10));
                Assert.That(summary.AssignedQuantity, Is.EqualTo(7));
                Assert.That(summary.UnassignedQuantity, Is.EqualTo(3));
            }
        });
    }

    [Test]
    public async Task PartiallyAllocatedItem_AppearsInUnassignedFilterAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 10));
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 7);
        }

        var specification = new ItemListSpecification(ItemQueryFilter.Unassigned);
        var sqliteItems = await sqlite.Query.QueryInventorySnapshotsAsync(specification);
        var jsonItems = await json.Query.QueryInventorySnapshotsAsync(specification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItems.Single().UnassignedQuantity, Is.EqualTo(3));
            Assert.That(jsonItems.Single().UnassignedQuantity, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task SetContainerAllocationAsync_AboveTotal_PersistsRaisedTotalAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 1);
        }

        var sqliteService = new ItemInventoryCommandService(sqlite.Query, sqlite.Command);
        var jsonService = new ItemInventoryCommandService(json.Query, json.Command);

        await sqliteService.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);
        await jsonService.SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        var sqliteItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(4));
            Assert.That(sqliteItem.AssignedQuantity, Is.EqualTo(4));
            Assert.That(sqliteItem.UnassignedQuantity, Is.Zero);
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(4));
            Assert.That(jsonItem.AssignedQuantity, Is.EqualTo(4));
            Assert.That(jsonItem.UnassignedQuantity, Is.Zero);
        });
    }

    [Test]
    public async Task AssigningOnePieceItemToContainer_DoesNotDuplicateItemAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 1));
        }

        await new AssignItemToContainerCommandHandler(new ItemInventoryCommandService(sqlite.Query, sqlite.Command))
            .AssignAsync(item.ItemId, container.ContainerId);
        await new AssignItemToContainerCommandHandler(new ItemInventoryCommandService(json.Query, json.Command))
            .AssignAsync(item.ItemId, container.ContainerId);

        var allItemsSpecification = new ItemListSpecification(ItemQueryFilter.All);
        var containerItemsSpecification = new ContainerItemsSpecification(container.ContainerId.ToString());
        var unassignedSpecification = new ItemListSpecification(ItemQueryFilter.Unassigned);

        var sqliteItems = await sqlite.Query.QueryInventorySnapshotsAsync(allItemsSpecification);
        var jsonItems = await json.Query.QueryInventorySnapshotsAsync(allItemsSpecification);
        var sqliteContainerItems = await sqlite.Query.QueryContainerItemInventoryAsync(containerItemsSpecification);
        var jsonContainerItems = await json.Query.QueryContainerItemInventoryAsync(containerItemsSpecification);
        var sqliteUnassignedItems = await sqlite.Query.QueryInventorySnapshotsAsync(unassignedSpecification);
        var jsonUnassignedItems = await json.Query.QueryInventorySnapshotsAsync(unassignedSpecification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItems.Select(snapshot => snapshot.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(jsonItems.Select(snapshot => snapshot.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(sqliteContainerItems.Select(entry => entry.Inventory.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(jsonContainerItems.Select(entry => entry.Inventory.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(sqliteContainerItems.Single().ContainerQuantity, Is.EqualTo(1));
            Assert.That(jsonContainerItems.Single().ContainerQuantity, Is.EqualTo(1));
            Assert.That(sqliteUnassignedItems, Is.Empty);
            Assert.That(jsonUnassignedItems, Is.Empty);
        });
    }

    [Test]
    public async Task EditUnassignAndReassignSameContainer_StoresSingleAllocationAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 5));
        }

        foreach (var (query, command) in new (IInventoryQueryRepository Query, IInventoryCommandRepository Command)[]
        {
            (sqlite.Query, sqlite.Command),
            (json.Query, json.Command),
        })
        {
            var inventoryCommands = new ItemInventoryCommandService(query, command);
            var quantityService = new ContainerItemQuantityService(inventoryCommands);
            var assignHandler = new AssignItemToContainerCommandHandler(inventoryCommands);
            var currentContainer = (await query.GetContainerAsync(container.ContainerId.ToString()))!;

            await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 4);
            await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 0);
            await assignHandler.AssignAsync(item.ItemId, container.ContainerId, 1);
        }

        var specification = new ContainerItemsSpecification(container.ContainerId.ToString());
        var sqliteContainerItems = await sqlite.Query.QueryContainerItemInventoryAsync(specification);
        var jsonContainerItems = await json.Query.QueryContainerItemInventoryAsync(specification);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteContainerItems.Select(entry => entry.Inventory.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(jsonContainerItems.Select(entry => entry.Inventory.Item.ItemId), Is.EqualTo(new[] { item.ItemId }));
            Assert.That(sqliteContainerItems.Single().ContainerQuantity, Is.EqualTo(1));
            Assert.That(jsonContainerItems.Single().ContainerQuantity, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ContainerRowRefresh_AfterAllocationEdit_ReturnsLocalAndGlobalQuantities()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 1);
        }

        await new ItemInventoryCommandService(sqlite.Query, sqlite.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);
        await new ItemInventoryCommandService(json.Query, json.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 4);

        var specification = new ContainerItemsSpecification(container.ContainerId.ToString());
        var sqliteRow = (await sqlite.Query.QueryContainerItemInventoryAsync(specification)).Single();
        var jsonRow = (await json.Query.QueryContainerItemInventoryAsync(specification)).Single();

        Assert.Multiple(() =>
        {
            foreach (var row in new[] { sqliteRow, jsonRow })
            {
                Assert.That(row.ContainerQuantity, Is.EqualTo(4));
                Assert.That(row.Inventory.TotalQuantity, Is.EqualTo(4));
                Assert.That(row.Inventory.AssignedQuantity, Is.EqualTo(4));
                Assert.That(row.Inventory.UnassignedQuantity, Is.Zero);
            }
        });
    }

    [Test]
    public async Task RemovingAllocation_ReleasesAssignedQuantityAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 6));
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 4);
        }

        await new ItemInventoryCommandService(sqlite.Query, sqlite.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);
        await new ItemInventoryCommandService(json.Query, json.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);

        var sqliteItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(6));
            Assert.That(sqliteItem.AssignedQuantity, Is.Zero);
            Assert.That(sqliteItem.UnassignedQuantity, Is.EqualTo(6));
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(6));
            Assert.That(jsonItem.AssignedQuantity, Is.Zero);
            Assert.That(jsonItem.UnassignedQuantity, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task DeletingContainer_ReleasesAllocationsWithoutChangingItemTotalAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 6));
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 4);
            await command.DeleteContainerAsync(container.ContainerId.ToString());
        }

        var sqliteItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(6));
            Assert.That(sqliteItem.AssignedQuantity, Is.Zero);
            Assert.That(sqliteItem.UnassignedQuantity, Is.EqualTo(6));
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(6));
            Assert.That(jsonItem.AssignedQuantity, Is.Zero);
            Assert.That(jsonItem.UnassignedQuantity, Is.EqualTo(6));
        });
    }

    [Test]
    public async Task ApplyWithdrawalAsync_CommitsAllAllocationsAndTotalAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var box = new Container(Guid.NewGuid(), "Box", "");
        var drawer = new Container(Guid.NewGuid(), "Drawer", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(box);
            await command.InsertContainerAsync(drawer);
            await command.InsertItemAsync(item);
            await command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 10));
            await command.InsertItemContainerRelation(item.ItemId, box.ContainerId, 5);
            await command.InsertItemContainerRelation(item.ItemId, drawer.ContainerId, 5);
        }

        var allocations = new[]
        {
            new ItemContainerAllocation(box.ContainerId, box.Name, 1),
            new ItemContainerAllocation(drawer.ContainerId, drawer.Name, 5),
        };
        var plan = new ItemInventoryWithdrawalPlan(7, 6, 1, allocations, false);

        await new ItemInventoryCommandService(sqlite.Query, sqlite.Command).ApplyWithdrawalAsync(item.ItemId, plan);
        await new ItemInventoryCommandService(json.Query, json.Command).ApplyWithdrawalAsync(item.ItemId, plan);

        var sqliteItem = await sqlite.Query.GetInventorySnapshotAsync(item.ItemId);
        var jsonItem = await json.Query.GetInventorySnapshotAsync(item.ItemId);
        var sqliteAllocations = await sqlite.Query.GetItemContainerAllocationsAsync(item.ItemId);
        var jsonAllocations = await json.Query.GetItemContainerAllocationsAsync(item.ItemId);

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(7));
            Assert.That(sqliteItem.AssignedQuantity, Is.EqualTo(6));
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(7));
            Assert.That(jsonItem.AssignedQuantity, Is.EqualTo(6));
            Assert.That(sqliteAllocations.Select(a => a.Quantity), Is.EquivalentTo(new[] { 1, 5 }));
            Assert.That(jsonAllocations.Select(a => a.Quantity), Is.EquivalentTo(new[] { 1, 5 }));
        });
    }

    [Test]
    public async Task ApplyWithdrawalAsync_WhenStockIsExhausted_DeletesItemAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var item = new Item(Guid.NewGuid(), "Widget", "");

        await sqlite.Command.InsertItemAsync(item);
        await json.Command.InsertItemAsync(item);
        await sqlite.Command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 1));
        await json.Command.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 1));

        var plan = new ItemInventoryWithdrawalPlan(0, 0, 0, [], true);
        await new ItemInventoryCommandService(sqlite.Query, sqlite.Command).ApplyWithdrawalAsync(item.ItemId, plan);
        await new ItemInventoryCommandService(json.Query, json.Command).ApplyWithdrawalAsync(item.ItemId, plan);

        Assert.That(await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString()), Is.Null);
        Assert.That(await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString()), Is.Null);
    }

    [Test]
    public async Task GetItemContainerAllocationsAsync_BulkLookupGroupsAndOrdersAllocationsAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var drawer = new Container(Guid.NewGuid(), "A Drawer", "");
        var box = new Container(Guid.NewGuid(), "Z Box", "");
        var firstItem = new Item(Guid.NewGuid(), "First", "");
        var secondItem = new Item(Guid.NewGuid(), "Second", "");
        var unassignedItem = new Item(Guid.NewGuid(), "Unassigned", "");

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(drawer);
            await command.InsertContainerAsync(box);
            await command.InsertItemAsync(firstItem);
            await command.InsertItemAsync(secondItem);
            await command.InsertItemAsync(unassignedItem);
            await command.InsertItemInventoryAsync(new ItemInventory(firstItem.ItemId, 10));
            await command.InsertItemInventoryAsync(new ItemInventory(secondItem.ItemId, 5));
            await command.InsertItemInventoryAsync(new ItemInventory(unassignedItem.ItemId, 1));
            await command.InsertItemContainerRelation(firstItem.ItemId, box.ContainerId, 3);
            await command.InsertItemContainerRelation(firstItem.ItemId, drawer.ContainerId, 2);
            await command.InsertItemContainerRelation(secondItem.ItemId, box.ContainerId, 5);
        }

        var itemIds = new[] { firstItem.ItemId, secondItem.ItemId, unassignedItem.ItemId };
        var sqliteAllocations = await sqlite.Query.GetItemContainerAllocationsAsync(itemIds);
        var jsonAllocations = await json.Query.GetItemContainerAllocationsAsync(itemIds);

        foreach (var allocations in new[] { sqliteAllocations, jsonAllocations })
        {
            Assert.Multiple(() =>
            {
                Assert.That(allocations.Keys, Is.EquivalentTo(new[] { firstItem.ItemId, secondItem.ItemId }));
                Assert.That(allocations[firstItem.ItemId].Select(allocation => allocation.ContainerName),
                    Is.EqualTo(new[] { drawer.Name, box.Name }));
                Assert.That(allocations[firstItem.ItemId].Select(allocation => allocation.Quantity),
                    Is.EqualTo(new[] { 2, 3 }));
                Assert.That(allocations[secondItem.ItemId].Single().Quantity, Is.EqualTo(5));
            });
        }
    }

    [Test]
    public async Task QueryContainerItemsWithPhotosAsync_PagesByRelationInsertionOrder()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var items = new[]
        {
            new Item(Guid.NewGuid(), "First", ""),
            new Item(Guid.NewGuid(), "Second", ""),
            new Item(Guid.NewGuid(), "Third", ""),
        };

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            foreach (var item in items)
            {
                await command.InsertItemAsync(item);
                await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 1);
            }
        }

        var specification = new ContainerItemsSpecification(
            container.ContainerId.ToString(),
            PageNumber: 1,
            PageSize: 1);

        var sqliteItems = await sqlite.Query.QueryContainerItemsWithPhotosAsync(specification);
        var jsonItems = await json.Query.QueryContainerItemsWithPhotosAsync(specification);

        Assert.That(sqliteItems.Select(i => i.ItemId), Is.EqualTo(new[] { items[1].ItemId }));
        Assert.That(jsonItems.Select(i => i.ItemId), Is.EqualTo(new[] { items[1].ItemId }));
    }

    [Test]
    public async Task QueryContainerItemsWithPhotosAsync_InvalidId_ReturnsEmpty_ForSqliteAndJson()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var specification = new ContainerItemsSpecification("not-a-guid");

        var sqliteItems = await sqlite.Query.QueryContainerItemsWithPhotosAsync(specification);
        var jsonItems = await json.Query.QueryContainerItemsWithPhotosAsync(specification);

        Assert.That(sqliteItems, Is.Empty);
        Assert.That(jsonItems, Is.Empty);
    }

    [Test]
    public async Task QueryContainerItemsWithPhotosAsync_DuplicateRelations_ParitiesAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var sqliteContainer = new Container(Guid.NewGuid(), "C1", "");
        var jsonContainer = new Container(Guid.NewGuid(), "C1", "");
        await sqlite.Command.InsertContainerAsync(sqliteContainer);
        await json.Command.InsertContainerAsync(jsonContainer);

        var sqliteItem = new Item("Hat", "Desc");
        var jsonItem = new Item("Hat", "Desc");
        await sqlite.Command.InsertItemAsync(sqliteItem);
        await json.Command.InsertItemAsync(jsonItem);

        await sqlite.Command.InsertItemContainerRelation(sqliteItem.ItemId, sqliteContainer.ContainerId, 1);
        await sqlite.Command.InsertItemContainerRelation(sqliteItem.ItemId, sqliteContainer.ContainerId, 1);
        await json.Command.InsertItemContainerRelation(jsonItem.ItemId, jsonContainer.ContainerId, 1);
        await json.Command.InsertItemContainerRelation(jsonItem.ItemId, jsonContainer.ContainerId, 1);

        var sqliteResults = await sqlite.Query.QueryContainerItemsWithPhotosAsync(
            new ContainerItemsSpecification(
                sqliteContainer.ContainerId.ToString(),
                SearchTerm: "hat",
                PageNumber: 0,
                PageSize: 10));

        var jsonResults = await json.Query.QueryContainerItemsWithPhotosAsync(
            new ContainerItemsSpecification(
                jsonContainer.ContainerId.ToString(),
                SearchTerm: "hat",
                PageNumber: 0,
                PageSize: 10));

        Assert.That(sqliteResults.Count, Is.EqualTo(1));
        Assert.That(jsonResults.Count, Is.EqualTo(1));
        Assert.That(sqliteResults.Select(i => i.Name), Is.EqualTo(new[] { "Hat" }));
        Assert.That(jsonResults.Select(i => i.Name), Is.EqualTo(new[] { "Hat" }));
    }

    private static async Task<SqliteHarness> BuildSqliteAsync()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"mothball-parity-{Guid.NewGuid():N}.db");
        var db = new MothballDatabase(dbPath);

        var containers = new Repository<DbContainer>(db);
        var items = new Repository<DbItem>(db);
        var inventories = new Repository<DbItemInventory>(db);
        var photos = new Repository<DbImage>(db);
        var relations = new Repository<DbItemContainerRelation>(db);
        await db.InitializeAsync();

        var containerLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContainerRepository>();
        var itemLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ItemRepository>();
        var transactionRunner = new SqliteTransactionRunner(db);

        var containerRepo = new ContainerRepository(transactionRunner, containers, photos, relations, containerLogger);
        var itemRepo = new ItemRepository(transactionRunner, items, photos, relations, itemLogger);
        var itemInventoryRepo = new ItemInventoryRepository(inventories, relations, containers, transactionRunner);
        var imageRepo = new ImageRepository(photos);
        var relationRepo = new RelationRepository(relations, transactionRunner);

        var query = new InventoryQueryRepository(containerRepo, itemRepo, itemInventoryRepo);
        var command = new InventoryCommandRepository(containerRepo, itemRepo, itemInventoryRepo, imageRepo, relationRepo);

        return new SqliteHarness(dbPath, db, query, command);
    }

    private static void AssertInventorySnapshot(InventorySnapshot item)
    {
        Assert.That(item.TotalQuantity, Is.EqualTo(12));
        Assert.That(item.AssignedQuantity, Is.EqualTo(7));
        Assert.That(item.UnassignedQuantity, Is.EqualTo(5));
    }

    private static async Task<JsonHarness> BuildJsonAsync()
    {
        var files = CreateInMemoryJsonFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        await store.TryRecoverAsync();

        var containerRepo = new JsonContainerRepository(store);
        var itemRepo = new JsonItemRepository(store);
        var itemInventoryRepo = new JsonItemInventoryRepository(store);
        var imageRepo = new JsonImageRepository(store);
        var relationRepo = new JsonRelationRepository(store);

        var query = new InventoryQueryRepository(containerRepo, itemRepo, itemInventoryRepo);
        var command = new InventoryCommandRepository(containerRepo, itemRepo, itemInventoryRepo, imageRepo, relationRepo);

        return new JsonHarness(query, command);
    }

    private static IFileHandler CreateInMemoryJsonFileHandler()
    {
        var textFiles = new Dictionary<(string folder, string file), string>();

        var mock = new Mock<IFileHandler>();
        mock.SetupGet(m => m.AppDataPath).Returns("/appdata");

        mock.Setup(m => m.SaveFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>()))
            .Throws(new NotSupportedException());
        mock.Setup(m => m.CopyFileFromRawToAppDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new NotSupportedException());
        mock.Setup(m => m.ReadFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new NotSupportedException());

        mock.Setup(m => m.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string fileName, string folderPath) =>
            {
                textFiles.Remove((folderPath, fileName));
                return Task.CompletedTask;
            });

        mock.Setup(m => m.SaveTextFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string fileName, string folderPath, string content) =>
            {
                textFiles[(folderPath, fileName)] = content;
                return Task.FromResult($"/appdata/{folderPath}/{fileName}");
            });

        mock.Setup(m => m.ReadTextFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string fileName, string folderPath) =>
            {
                if (!textFiles.TryGetValue((folderPath, fileName), out var content))
                {
                    throw new FileNotFoundException($"Missing file: {folderPath}/{fileName}");
                }

                return Task.FromResult(content);
            });

        mock.Setup(m => m.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string folderPath, string _) =>
                textFiles.Keys
                    .Where(k => k.folder == folderPath)
                    .Select(k => k.file)
                    .Distinct()
                    .ToList());

        return mock.Object;
    }

    private sealed class SqliteHarness : IAsyncDisposable
    {
        private readonly string dbPath;
        private readonly MothballDatabase db;

        public SqliteHarness(string dbPath, MothballDatabase db, IInventoryQueryRepository query, IInventoryCommandRepository command)
        {
            this.dbPath = dbPath;
            this.db = db;
            Query = query;
            Command = command;
        }

        public IInventoryQueryRepository Query { get; }
        public IInventoryCommandRepository Command { get; }

        public async ValueTask DisposeAsync()
        {
            await db.DisposeAsync();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private sealed class JsonHarness
    {
        public JsonHarness(IInventoryQueryRepository query, IInventoryCommandRepository command)
        {
            Query = query;
            Command = command;
        }

        public IInventoryQueryRepository Query { get; }
        public IInventoryCommandRepository Command { get; }
    }

}
