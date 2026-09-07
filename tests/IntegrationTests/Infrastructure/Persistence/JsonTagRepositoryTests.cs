using CoreApp.Application.Contracts.Tags;
using CoreApp.Application.Abstractions.Platform;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Restore;
using CoreApp.Application.Contracts.Backup;
using CoreApp.Application.Features.Backup.Restore.Planning;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public class JsonTagRepositoryTests
{
    private sealed class InMemoryFileHandler : IFileHandler
    {
        private readonly Dictionary<(string folder, string file), string> files = new();

        public string AppDataPath => "/appdata";
        public bool FileExists(string fileName, string folderPath) => files.ContainsKey((folderPath, fileName));
        public Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath) => throw new NotSupportedException();
        public Task<byte[]> ReadFileAsync(string fileName, string folderPath) => throw new NotSupportedException();
        public Task DeleteFileAsync(string fileName, string folderPath, CancellationToken cancellationToken = default) { files.Remove((folderPath, fileName)); return Task.CompletedTask; }
        public Task<string> SaveTextFileAsync(string fileName, string folderPath, string content) { files[(folderPath, fileName)] = content; return Task.FromResult($"{AppDataPath}/{folderPath}/{fileName}"); }
        public Task<string> ReadTextFileAsync(string fileName, string folderPath) => Task.FromResult(files[(folderPath, fileName)]);
        public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*") => files.Keys.Where(key => key.folder == folderPath).Select(key => key.file).ToList();
    }

    [Test]
    public async Task GetOrCreateAndAssignAsync_PreservesTagSemantics()
    {
        var store = new JsonInventoryStore(new InMemoryFileHandler(), NullLogger<JsonInventoryStore>.Instance);
        var repository = new JsonTagRepository(store);
        var itemId = Guid.NewGuid();

        var first = await repository.GetOrCreateAsync(new TagName("#Winter"));
        var second = await repository.GetOrCreateAsync(new TagName("winter"));
        await repository.AssignAsync(first.TagId, TagTargetType.Item, itemId);
        await repository.AssignAsync(first.TagId, TagTargetType.Item, itemId);

        var assigned = await repository.GetForTargetAsync(TagTargetType.Item, itemId);

        Assert.That(second.TagId, Is.EqualTo(first.TagId));
        Assert.That(assigned.Select(tag => tag.TagId), Is.EqualTo(new[] { first.TagId }));
    }

    [Test]
    public async Task GetAllAsync_ReturnsTagsInDisplayOrder()
    {
        var store = new JsonInventoryStore(new InMemoryFileHandler(), NullLogger<JsonInventoryStore>.Instance);
        var repository = new JsonTagRepository(store);

        await repository.GetOrCreateAsync(new TagName("zebra"));
        await repository.GetOrCreateAsync(new TagName("Alpha"));

        var tags = await repository.GetAllAsync();

        Assert.That(tags.Select(tag => tag.Name.Value), Is.EqualTo(new[] { "Alpha", "zebra" }));
    }

    [Test]
    public async Task GetUsageSummariesAsync_ReturnsItemAndContainerCounts()
    {
        var store = new JsonInventoryStore(new InMemoryFileHandler(), NullLogger<JsonInventoryStore>.Instance);
        var repository = new JsonTagRepository(store);
        var tag = await repository.GetOrCreateAsync(new TagName("winter"));

        await repository.AssignAsync(tag.TagId, TagTargetType.Item, Guid.NewGuid());
        await repository.AssignAsync(tag.TagId, TagTargetType.Container, Guid.NewGuid());

        var summary = (await repository.GetUsageSummariesAsync()).Single();

        Assert.That(summary.ItemCount, Is.EqualTo(1));
        Assert.That(summary.ContainerCount, Is.EqualTo(1));
        Assert.That(summary.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task RestoreAsync_RestoresTagDefinitionsAndAssignments()
    {
        var store = new JsonInventoryStore(new InMemoryFileHandler(), NullLogger<JsonInventoryStore>.Instance);
        var service = new JsonInventoryBackupRestoreService(store);
        var tagId = Guid.NewGuid();
        var containerId = Guid.NewGuid();
        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "Garage" },
                ],
                Tags =
                [
                    new InventoryBackupTag { TagId = tagId, Name = "#Winter" },
                ],
                TagAssignments =
                [
                    new InventoryBackupTagAssignment
                    {
                        TagId = tagId,
                        TargetId = containerId,
                        TargetType = TagTargetType.Container,
                    },
                ],
            },
        });

        await service.RestoreAsync(backup);

        var repository = new JsonTagRepository(store);
        var assigned = await repository.GetForTargetAsync(TagTargetType.Container, containerId);
        Assert.That(assigned.Select(tag => tag.Name.Value), Is.EqualTo(new[] { "Winter" }));
    }
}
