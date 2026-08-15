using System.Text.Json;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Specifications;
using Infrastructure.Interfaces;
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
    public async Task ItemTotalQuantity_PersistsAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 12);

        await sqlite.Command.InsertItemAsync(item);
        await json.Command.InsertItemAsync(item);

        var sqliteItem = await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString());
        var jsonItem = await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString());

        Assert.Multiple(() =>
        {
            Assert.That(sqliteItem!.TotalQuantity, Is.EqualTo(12));
            Assert.That(jsonItem!.TotalQuantity, Is.EqualTo(12));
        });
    }

    [Test]
    public async Task ItemInventorySummary_AggregatesAllocationsAcrossContainers()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var firstContainer = new Container(Guid.NewGuid(), "Box", "");
        var secondContainer = new Container(Guid.NewGuid(), "Drawer", "");
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 12);

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(firstContainer);
            await command.InsertContainerAsync(secondContainer);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, firstContainer.ContainerId, 3);
            await command.InsertItemContainerRelation(item.ItemId, secondContainer.ContainerId, 4);
        }

        var sqliteListItem = (await sqlite.Query.QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))).Single();
        var jsonListItem = (await json.Query.QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))).Single();
        var sqliteDetailsItem = await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString());
        var jsonDetailsItem = await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString());

        Assert.Multiple(() =>
        {
            AssertInventorySummary(sqliteListItem);
            AssertInventorySummary(jsonListItem);
            AssertInventorySummary(sqliteDetailsItem!);
            AssertInventorySummary(jsonDetailsItem!);
        });
    }

    [Test]
    public async Task SetContainerAllocationAsync_AboveTotal_PersistsRaisedTotalAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 2);

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

        var sqliteItem = await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString());
        var jsonItem = await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString());

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
    public async Task RemovingAllocation_ReleasesAssignedQuantityAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 6);

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 4);
        }

        await new ItemInventoryCommandService(sqlite.Query, sqlite.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);
        await new ItemInventoryCommandService(json.Query, json.Command)
            .SetContainerAllocationAsync(item.ItemId, container.ContainerId, 0);

        var sqliteItem = await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString());
        var jsonItem = await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString());

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
        var item = new Item(Guid.NewGuid(), "Widget", "", totalQuantity: 6);

        foreach (var command in new[] { sqlite.Command, json.Command })
        {
            await command.InsertContainerAsync(container);
            await command.InsertItemAsync(item);
            await command.InsertItemContainerRelation(item.ItemId, container.ContainerId, 4);
            await command.DeleteContainerAsync(container.ContainerId.ToString());
        }

        var sqliteItem = await sqlite.Query.GetItemWithPhotosAsync(item.ItemId.ToString());
        var jsonItem = await json.Query.GetItemWithPhotosAsync(item.ItemId.ToString());

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

        var sqliteItem = new Item("Hat", "Desc", totalQuantity: 2);
        var jsonItem = new Item("Hat", "Desc", totalQuantity: 2);
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

        Assert.That(sqliteResults.Count, Is.EqualTo(2));
        Assert.That(jsonResults.Count, Is.EqualTo(2));
        Assert.That(sqliteResults.Select(i => i.Name), Is.EqualTo(new[] { "Hat", "Hat" }));
        Assert.That(jsonResults.Select(i => i.Name), Is.EqualTo(new[] { "Hat", "Hat" }));
    }

    private static async Task<SqliteHarness> BuildSqliteAsync()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), $"mothball-parity-{Guid.NewGuid():N}.db");
        var db = new MothballDatabase(dbPath);

        var containers = new Repository<DbContainer>(db);
        var items = new Repository<DbItem>(db);
        var photos = new Repository<DbImage>(db);
        var relations = new Repository<DbItemContainerRelation>(db);
        await db.InitializeAsync();

        var containerLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ContainerRepository>();
        var itemLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<ItemRepository>();
        var transactionRunner = new SqliteTransactionRunner(db);

        var containerRepo = new ContainerRepository(transactionRunner, containers, photos, relations, containerLogger);
        var itemRepo = new ItemRepository(transactionRunner, items, photos, relations, itemLogger);
        var imageRepo = new ImageRepository(photos);
        var relationRepo = new RelationRepository(relations, transactionRunner);

        var query = new InventoryQueryRepository(containerRepo, itemRepo);
        var command = new InventoryCommandRepository(containerRepo, itemRepo, imageRepo, relationRepo);

        return new SqliteHarness(dbPath, db, query, command);
    }

    private static void AssertInventorySummary(Item item)
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
        var imageRepo = new JsonImageRepository(store);
        var relationRepo = new JsonRelationRepository(store);

        var query = new InventoryQueryRepository(containerRepo, itemRepo);
        var command = new InventoryCommandRepository(containerRepo, itemRepo, imageRepo, relationRepo);

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
