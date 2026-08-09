using System.Text.Json;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;
using Infrastructure.Services;
using Infrastructure.Services.DatabaseModels;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Moq;

namespace UnitTests;

[TestFixture]
public class BackendParityTests
{
    [Test]
    public async Task GetItemsForContainerAsync_InvalidId_ReturnsEmpty_ForSqliteAndJson()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var sqliteItems = await sqlite.Query.GetItemsForContainerAsync("not-a-guid");
        var jsonItems = await json.Query.GetItemsForContainerAsync("not-a-guid");

        Assert.That(sqliteItems, Is.Empty);
        Assert.That(jsonItems, Is.Empty);
    }

    [Test]
    public async Task SearchItemsInContainerAsync_DuplicateRelations_ParitiesAcrossBackends()
    {
        await using var sqlite = await BuildSqliteAsync();
        var json = await BuildJsonAsync();

        var sqliteContainer = new Container(Guid.NewGuid(), "C1", "");
        var jsonContainer = new Container(Guid.NewGuid(), "C1", "");
        await sqlite.Command.InsertContainerAsync(sqliteContainer);
        await json.Command.InsertContainerAsync(jsonContainer);

        var sqliteItem = new Item { Name = "Hat", Description = "Desc" };
        var jsonItem = new Item { Name = "Hat", Description = "Desc" };
        await sqlite.Command.InsertItemAsync(sqliteItem);
        await json.Command.InsertItemAsync(jsonItem);

        await sqlite.Command.InsertItemContainerRelation(sqliteItem.ItemId, sqliteContainer.ContainerId, 1);
        await sqlite.Command.InsertItemContainerRelation(sqliteItem.ItemId, sqliteContainer.ContainerId, 1);
        await json.Command.InsertItemContainerRelation(jsonItem.ItemId, jsonContainer.ContainerId, 1);
        await json.Command.InsertItemContainerRelation(jsonItem.ItemId, jsonContainer.ContainerId, 1);

        var sqliteResults = await sqlite.Query.SearchItemsInContainerAsync(
            sqliteContainer.ContainerId.ToString(),
            "hat",
            pageNumber: 0,
            pageSize: 10);

        var jsonResults = await json.Query.SearchItemsInContainerAsync(
            jsonContainer.ContainerId.ToString(),
            "hat",
            pageNumber: 0,
            pageSize: 10);

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
        var relationRepo = new RelationRepository(relations);

        var query = new InventoryQueryRepository(containerRepo, itemRepo);
        var command = new InventoryCommandRepository(containerRepo, itemRepo, imageRepo, relationRepo);

        return new SqliteHarness(dbPath, db, query, command);
    }

    private static async Task<JsonHarness> BuildJsonAsync()
    {
        var files = CreateInMemoryJsonFileHandler();
        var store = new JsonInventoryStore(files);
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
