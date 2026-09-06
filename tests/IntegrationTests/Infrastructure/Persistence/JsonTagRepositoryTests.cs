using CoreApp.Application.Contracts.Tags;
using CoreApp.Application.Abstractions.Platform;
using CoreApp.Domain.ValueObjects;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
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
}
