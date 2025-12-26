using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;

namespace UnitTests;

[TestFixture]
public class JsonOperationalStoreTests
{
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
    }

    [Test]
    public async Task TryRecoverAsync_FirstRun_CreatesReadableStore()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files);

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
        var store = new JsonInventoryStore(files);
        var maintenance = new JsonInventoryMaintenanceService(store);

        var containers = new JsonContainerRepository(store);

        Assert.That(await maintenance.TryRecoverAsync(), Is.True);

        var c1 = new Container(Guid.NewGuid(), "Box", "N1");
        var c2 = new Container(Guid.NewGuid(), "Crate", "N2");

        await containers.InsertAsync(c1);
        var afterFirst = await containers.GetAllAsync();
        Assert.That(afterFirst.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId }));

        await containers.InsertAsync(c2);
        var afterSecond = await containers.GetAllAsync();
        Assert.That(afterSecond.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId, c2.ContainerId }));

        var rolledBack = await maintenance.TryRollbackLastCommitAsync();
        Assert.That(rolledBack, Is.True);

        var afterRollback = await containers.GetAllAsync();
        Assert.That(afterRollback.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId }));
    }
}
