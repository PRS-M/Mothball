using CoreApp.Entities.ContainerAggregate;
using FluentAssertions;
using CoreApp.Interfaces;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Models;
using Infrastructure.Services.JsonStore.Repositories;
using System.Text.Json;

namespace UnitTests;

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
        var store = new JsonInventoryStore(files);

        var ok = await store.TryRecoverAsync();
        ok.Should().BeTrue();

        var state = await store.LoadAsync();
        state.Containers.Should().BeEmpty();
        state.Items.Should().BeEmpty();
        state.Images.Should().BeEmpty();
        state.Relations.Should().BeEmpty();
    }

    [Test]
    public async Task Rollback_RevertsLastCommit_MetadataOnly()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);
        var maintenance = new JsonInventoryMaintenanceService(store);

        var containers = new JsonContainerRepository(store);

        (await maintenance.TryRecoverAsync()).Should().BeTrue();

        var c1 = new Container(Guid.NewGuid(), "Box", "N1");
        var c2 = new Container(Guid.NewGuid(), "Crate", "N2");

        await containers.InsertAsync(c1);
        var afterFirst = await containers.GetAllAsync();
        afterFirst.Select(c => c.ContainerId).Should().BeEquivalentTo(new[] { c1.ContainerId });

        await containers.InsertAsync(c2);
        var afterSecond = await containers.GetAllAsync();
        afterSecond.Select(c => c.ContainerId).Should().BeEquivalentTo(new[] { c1.ContainerId, c2.ContainerId });

        var rolledBack = await maintenance.TryRollbackLastCommitAsync();
        rolledBack.Should().BeTrue();

        var afterRollback = await containers.GetAllAsync();
        afterRollback.Select(c => c.ContainerId).Should().BeEquivalentTo(new[] { c1.ContainerId });
    }

    [Test]
    public async Task TryRecoverAsync_WhenStoreAlreadyValid_IsIdempotent()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);

        (await store.TryRecoverAsync()).Should().BeTrue();

        await store.UpdateAsync(state =>
        {
            state.Metadata.SchemaVersion = 42;
            return Task.CompletedTask;
        });

        (await store.TryRecoverAsync()).Should().BeTrue();

        var loaded = await store.LoadAsync();
        loaded.Metadata.SchemaVersion.Should().Be(42);
    }

    [Test]
    public async Task LoadAsync_WithoutManifest_AutoRecoversToEmptyStore()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);

        var loaded = await store.LoadAsync();

        loaded.Containers.Should().BeEmpty();
        loaded.Items.Should().BeEmpty();
        loaded.Images.Should().BeEmpty();
        loaded.Relations.Should().BeEmpty();
        loaded.Metadata.NextContainerRowId.Should().Be(1);
    }

    [Test]
    public async Task TryRollbackLastCommitAsync_FirstGeneration_ReturnsFalse()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);

        (await store.TryRecoverAsync()).Should().BeTrue();

        var rolledBack = await store.TryRollbackLastCommitAsync();
        rolledBack.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_WhenUpdaterThrows_DoesNotCommitPartialMutation()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);
        (await store.TryRecoverAsync()).Should().BeTrue();

        await FluentActions.Awaiting(() => store.UpdateAsync(_ => throw new InvalidOperationException("boom")))
            .Should()
            .ThrowAsync<InvalidOperationException>();

        var loaded = await store.LoadAsync();
        loaded.Metadata.SchemaVersion.Should().Be(1);

        await FluentActions.Awaiting(() => store.UpdateAsync(state =>
            {
                state.Metadata.SchemaVersion = 2;
                return Task.CompletedTask;
            }))
            .Should()
            .NotThrowAsync();

        var after = await store.LoadAsync();
        after.Metadata.SchemaVersion.Should().Be(2);
    }

    [Test]
    public async Task LoadAsync_WhenCurrentSlotIsIncomplete_FallsBackToPreviousSlot()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);
        var containers = new JsonContainerRepository(store);

        (await store.TryRecoverAsync()).Should().BeTrue();

        var c1 = new Container(Guid.NewGuid(), "Box", "N1");
        var c2 = new Container(Guid.NewGuid(), "Crate", "N2");

        await containers.InsertAsync(c1);
        await containers.InsertAsync(c2);

        var activeManifest = ReadHighestGenerationManifest(files);
        activeManifest.Should().NotBeNull();

        var currentSlotFolder = JsonStoreConstants.SlotFolder(activeManifest!.CurrentSlot);
        files.DeleteRaw(JsonStoreConstants.MetadataFileName, currentSlotFolder);

        var loaded = await containers.GetAllAsync();
        loaded.Select(x => x.ContainerId).Should().BeEquivalentTo(new[] { c1.ContainerId });
    }

    [Test]
    public async Task LoadAsync_WhenMetadataCountersAreStale_RecomputesFromRows()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);

        await SeedSlotAAsync(files);

        var loaded = await store.LoadAsync();

        loaded.Metadata.NextContainerRowId.Should().Be(8);
        loaded.Metadata.NextItemRowId.Should().Be(4);
        loaded.Metadata.NextImageRowId.Should().Be(6);
        loaded.Metadata.NextRelationId.Should().Be(10);
    }

    [Test]
    public async Task StartupInitializer_WhenRecoverFails_ThrowsInvalidOperationException()
    {
        var store = new JsonInventoryStore(new FailingWriteFileHandler());
        var initializer = new JsonStoreStartupInitializer(store);

        await FluentActions.Awaiting(() => initializer.InitializeAsync())
            .Should()
            .ThrowAsync<InvalidOperationException>();
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
