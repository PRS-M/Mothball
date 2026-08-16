using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Inventory;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Specifications;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public class JsonOperationalStoreTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private sealed class InMemoryFileHandler : IFileHandler
    {
        private readonly Dictionary<(string folder, string file), string> textFiles = new();

        public string AppDataPath => "/appdata";

        public Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
            => throw new NotSupportedException();

        public Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath)
            => throw new NotSupportedException();

        public Task<byte[]> ReadFileAsync(string fileName, string folderPath)
            => throw new NotSupportedException();

        public Task DeleteFileAsync(string fileName, string folderPath)
        {
            textFiles.Remove((folderPath, fileName));
            return Task.CompletedTask;
        }

        public Task<string> SaveTextFileAsync(string fileName, string folderPath, string content)
        {
            textFiles[(folderPath, fileName)] = content;
            return Task.FromResult($"{AppDataPath}/{folderPath}/{fileName}");
        }

        public Task<string> ReadTextFileAsync(string fileName, string folderPath)
        {
            if (!textFiles.TryGetValue((folderPath, fileName), out var content))
            {
                throw new FileNotFoundException($"Missing file: {folderPath}/{fileName}");
            }

            return Task.FromResult(content);
        }

        public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*")
            => textFiles.Keys.Where(k => k.folder == folderPath).Select(k => k.file).Distinct().ToList();

        public async Task WriteRawAsync(string fileName, string folderPath, string content)
        {
            await SaveTextFileAsync(fileName, folderPath, content);
        }

        public void DeleteRaw(string fileName, string folderPath)
        {
            textFiles.Remove((folderPath, fileName));
        }

        public string? TryReadRaw(string fileName, string folderPath)
        {
            return textFiles.TryGetValue((folderPath, fileName), out var content) ? content : null;
        }
    }

    private sealed class FailingWriteFileHandler : IFileHandler
    {
        public string AppDataPath => "/appdata";

        public Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
            => throw new NotSupportedException();

        public Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath)
            => throw new NotSupportedException();

        public Task<byte[]> ReadFileAsync(string fileName, string folderPath)
            => throw new NotSupportedException();

        public Task DeleteFileAsync(string fileName, string folderPath)
            => Task.CompletedTask;

        public Task<string> SaveTextFileAsync(string fileName, string folderPath, string content)
            => throw new IOException("Simulated write failure");

        public Task<string> ReadTextFileAsync(string fileName, string folderPath)
            => throw new FileNotFoundException();

        public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*")
            => [];
    }

    [Test]
    public async Task TryRecoverAsync_FirstRun_CreatesReadableStore()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);

        var ok = await store.TryRecoverAsync();
        Assert.That(ok, Is.True);

        var state = await store.LoadAsync();
        Assert.That(state.Containers, Is.Empty);
        Assert.That(state.Items, Is.Empty);
        Assert.That(state.Images, Is.Empty);
        Assert.That(state.Relations, Is.Empty);
    }

    [Test]
    public async Task Rollback_RevertsLastCommit_MetadataOnly()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        var maintenance = new JsonInventoryMaintenanceService(store);

        var containers = new JsonContainerRepository(store);

        Assert.That(await maintenance.TryRecoverAsync(), Is.True);

        var c1 = new Container(Guid.NewGuid(), "Box", "N1");
        var c2 = new Container(Guid.NewGuid(), "Crate", "N2");

        await containers.InsertAsync(c1);
        var afterFirst = await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All));
        Assert.That(afterFirst.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId }));

        await containers.InsertAsync(c2);
        var afterSecond = await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All));
        Assert.That(afterSecond.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId, c2.ContainerId }));

        var rolledBack = await maintenance.TryRollbackLastCommitAsync();
        Assert.That(rolledBack, Is.True);

        var afterRollback = await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All));
        Assert.That(afterRollback.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId }));
    }

    [Test]
    public async Task TryRecoverAsync_WhenStoreAlreadyValid_IsIdempotent()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);

        Assert.That(await store.TryRecoverAsync(), Is.True);

        await store.UpdateAsync(state =>
        {
            state.Metadata.SchemaVersion = 42;
            return Task.CompletedTask;
        });

        Assert.That(await store.TryRecoverAsync(), Is.True);

        var loaded = await store.LoadAsync();
        Assert.That(loaded.Metadata.SchemaVersion, Is.EqualTo(42));
    }

    [Test]
    public async Task LoadAsync_WithoutManifest_AutoRecoversToEmptyStore()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);

        var loaded = await store.LoadAsync();

        Assert.That(loaded.Containers, Is.Empty);
        Assert.That(loaded.Items, Is.Empty);
        Assert.That(loaded.Images, Is.Empty);
        Assert.That(loaded.Relations, Is.Empty);
        Assert.That(loaded.Metadata.NextContainerRowId, Is.EqualTo(1));
    }

    [Test]
    public async Task TryRollbackLastCommitAsync_FirstGeneration_ReturnsFalse()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);

        Assert.That(await store.TryRecoverAsync(), Is.True);

        var rolledBack = await store.TryRollbackLastCommitAsync();
        Assert.That(rolledBack, Is.False);
    }

    [Test]
    public async Task UpdateAsync_WhenUpdaterThrows_DoesNotCommitPartialMutation()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        Assert.That(await store.TryRecoverAsync(), Is.True);

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await store.UpdateAsync(_ => throw new InvalidOperationException("boom")));

        var loaded = await store.LoadAsync();
        Assert.That(loaded.Metadata.SchemaVersion, Is.EqualTo(1));

        Assert.DoesNotThrowAsync(async () =>
            await store.UpdateAsync(state =>
            {
                state.Metadata.SchemaVersion = 1;
                return Task.CompletedTask;
            }));

        var after = await store.LoadAsync();
        Assert.That(after.Metadata.SchemaVersion, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadAsync_WhenCurrentSlotIsIncomplete_FallsBackToPreviousSlot()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        var containers = new JsonContainerRepository(store);

        Assert.That(await store.TryRecoverAsync(), Is.True);

        var c1 = new Container(Guid.NewGuid(), "Box", "N1");
        var c2 = new Container(Guid.NewGuid(), "Crate", "N2");

        await containers.InsertAsync(c1);
        await containers.InsertAsync(c2);

        var activeManifest = ReadHighestGenerationManifest(files);
        Assert.That(activeManifest, Is.Not.Null);

        var currentSlotFolder = JsonStoreConstants.SlotFolder(activeManifest!.CurrentSlot);
        files.DeleteRaw(JsonStoreConstants.MetadataFileName, currentSlotFolder);

        var loaded = await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All));
        Assert.That(loaded.Select(x => x.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId }));
    }

    [Test]
    public async Task LoadAsync_WhenMetadataCountersAreStale_RecomputesFromRows()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);

        await SeedSlotAAsync(files);

        var loaded = await store.LoadAsync();

        Assert.That(loaded.Metadata.NextContainerRowId, Is.EqualTo(8));
        Assert.That(loaded.Metadata.NextItemRowId, Is.EqualTo(4));
        Assert.That(loaded.Metadata.NextImageRowId, Is.EqualTo(6));
        Assert.That(loaded.Metadata.NextRelationId, Is.EqualTo(10));
        Assert.That(loaded.Metadata.SchemaVersion, Is.EqualTo(1));
    }

    [Test]
    public async Task RelationRepository_InsertSameItemContainerTwice_StoresSingleRelationRow()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        var relations = new JsonRelationRepository(store);
        var itemId = Guid.NewGuid();
        var containerId = Guid.NewGuid();

        Assert.That(await store.TryRecoverAsync(), Is.True);

        await relations.InsertItemContainerRelationAsync(itemId, containerId, 2);
        await relations.InsertItemContainerRelationAsync(itemId, containerId, 3);

        var state = await store.LoadAsync();
        var relationRows = state.Relations
            .Where(relation => relation.ItemId == itemId && relation.ContainerId == containerId)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(relationRows, Has.Count.EqualTo(1));
            Assert.That(relationRows.Single().Quantity, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task EditUnassignAndReassignSameContainer_StoresSingleRelationRow()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        var containerRepo = new JsonContainerRepository(store);
        var itemRepo = new JsonItemRepository(store);
        var itemInventoryRepo = new JsonItemInventoryRepository(store);
        var imageRepo = new JsonImageRepository(store);
        var relationRepo = new JsonRelationRepository(store);
        var queryRepo = new InventoryQueryRepository(containerRepo, itemRepo, itemInventoryRepo);
        var commandRepo = new InventoryCommandRepository(containerRepo, itemRepo, itemInventoryRepo, imageRepo, relationRepo);
        var inventoryCommands = new ItemInventoryCommandService(queryRepo, commandRepo);
        var quantityService = new ContainerItemQuantityService(inventoryCommands);
        var assignHandler = new AssignItemToContainerCommandHandler(inventoryCommands);
        var container = new Container(Guid.NewGuid(), "Box", "");
        var item = new Item(Guid.NewGuid(), "Widget", "");

        Assert.That(await store.TryRecoverAsync(), Is.True);
        await commandRepo.InsertContainerAsync(container);
        await commandRepo.InsertItemAsync(item);
        await commandRepo.InsertItemInventoryAsync(new ItemInventory(item.ItemId, 5));

        var currentContainer = (await queryRepo.GetContainerAsync(container.ContainerId.ToString()))!;
        await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 4);
        await quantityService.SaveQuantityAsync(currentContainer, item.ItemId, 0);
        await assignHandler.AssignAsync(item.ItemId, container.ContainerId, 1);

        var state = await store.LoadAsync();
        var relationRows = state.Relations
            .Where(relation => relation.ItemId == item.ItemId && relation.ContainerId == container.ContainerId)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(relationRows, Has.Count.EqualTo(1));
            Assert.That(relationRows.Single().Quantity, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task StartupInitializer_WhenRecoverFails_ThrowsInvalidOperationException()
    {
        var store = new JsonInventoryStore(new FailingWriteFileHandler(), NullLogger<JsonInventoryStore>.Instance);
        var initializer = new JsonStoreStartupInitializer(store);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await initializer.InitializeAsync());
    }

    private static JsonStoreManifest? ReadHighestGenerationManifest(InMemoryFileHandler files)
    {
        var a = TryReadManifest(files, JsonStoreConstants.ManifestAFileName);
        var b = TryReadManifest(files, JsonStoreConstants.ManifestBFileName);

        if (a is null) return b;
        if (b is null) return a;
        return a.Generation >= b.Generation ? a : b;
    }

    private static JsonStoreManifest? TryReadManifest(InMemoryFileHandler files, string fileName)
    {
        var raw = files.TryReadRaw(fileName, JsonStoreConstants.StoreRoot);
        return raw is null ? null : JsonSerializer.Deserialize<JsonStoreManifest>(raw, JsonOptions);
    }

    private static async Task SeedSlotAAsync(InMemoryFileHandler files)
    {
        var metadata = new JsonStoreMetadata
        {
            SchemaVersion = 1,
            NextContainerRowId = 1,
            NextItemRowId = 1,
            NextImageRowId = 1,
            NextRelationId = 1,
        };

        var containers = new List<JsonContainerRow>
        {
            new() { RowId = 7, ContainerId = Guid.NewGuid(), Name = "C7", Notes = "n" },
        };
        var items = new List<JsonItemRow>
        {
            new() { RowId = 2, ItemId = Guid.NewGuid(), Name = "I2", Description = "d" },
            new() { RowId = 3, ItemId = Guid.NewGuid(), Name = "I3", Description = "d" },
        };
        var inventories = new List<JsonInventoryRow>
        {
            new() { ItemId = items[0].ItemId, TotalQuantity = 1 },
            new() { ItemId = items[1].ItemId, TotalQuantity = 1 },
        };
        var images = new List<JsonImageRow>
        {
            new() { RowId = 5, ImageId = Guid.NewGuid(), OwnerUniqueId = containers[0].ContainerId },
        };
        var relations = new List<JsonRelationRow>
        {
            new() { Id = 9, ContainerId = containers[0].ContainerId, ItemId = items[0].ItemId, Quantity = 1 },
        };

        var commitInfo = new JsonStoreCommitInfo
        {
            Generation = 1,
            CommitId = Guid.NewGuid(),
            CommittedUtc = DateTimeOffset.UtcNow,
        };

        await files.WriteRawAsync(JsonStoreConstants.MetadataFileName, JsonStoreConstants.SlotA, Serialize(metadata));
        await files.WriteRawAsync(JsonStoreConstants.ContainersFileName, JsonStoreConstants.SlotA, Serialize(containers));
        await files.WriteRawAsync(JsonStoreConstants.ItemsFileName, JsonStoreConstants.SlotA, Serialize(items));
        await files.WriteRawAsync(JsonStoreConstants.InventoriesFileName, JsonStoreConstants.SlotA, Serialize(inventories));
        await files.WriteRawAsync(JsonStoreConstants.ImagesFileName, JsonStoreConstants.SlotA, Serialize(images));
        await files.WriteRawAsync(JsonStoreConstants.RelationsFileName, JsonStoreConstants.SlotA, Serialize(relations));
        await files.WriteRawAsync(JsonStoreConstants.CommitInfoFileName, JsonStoreConstants.SlotA, Serialize(commitInfo));

        var manifestA = new JsonStoreManifest
        {
            Generation = 1,
            CurrentSlot = "A",
            PreviousSlot = "A",
            SchemaVersion = 1,
        };

        await files.WriteRawAsync(JsonStoreConstants.ManifestAFileName, JsonStoreConstants.StoreRoot, Serialize(manifestA));
    }

    private static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, JsonOptions);
}
