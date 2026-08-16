using CoreApp.Contracts;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Specifications;
using CoreApp.Utilities;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Repositories;
using Infrastructure.Services.Restore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests;

[TestFixture]
public class JsonInventoryBackupRestoreServiceTests
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
    public async Task RestoreAsync_CommitsJsonRestoreAsSingleRollbackUnit()
    {
        var files = new InMemoryFileHandler();
        var store = new JsonInventoryStore(files, NullLogger<JsonInventoryStore>.Instance);
        var containers = new JsonContainerRepository(store);
        var items = new JsonItemRepository(store);
        var service = new JsonInventoryBackupRestoreService(store);

        Assert.That(await store.TryRecoverAsync(), Is.True);

        var originalContainerId = Guid.NewGuid();
        await containers.InsertAsync(new Container(originalContainerId, "Original", "Kept"));

        var restoredContainerId = Guid.NewGuid();
        var restoredItemId = Guid.NewGuid();
        var restoredImageId = Guid.NewGuid();
        var backup = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = restoredContainerId, Name = "Restored", Notes = "Notes" },
                ],
                Items =
                [
                    new InventoryBackupItem
                    {
                        ItemId = restoredItemId,
                        Name = "Item",
                        Description = "Description",
                        TotalQuantity = 2,
                    },
                ],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = restoredContainerId, ItemId = restoredItemId, Quantity = 2 },
                ],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        ImageId = restoredImageId,
                        OwnerId = restoredContainerId,
                        OwnerType = InventoryBackupOwnerType.Container,
                        FileName = $"{restoredImageId}.jpg",
                    },
                ],
            },
        });

        var result = await service.RestoreAsync(backup);

        Assert.Multiple(() =>
        {
            Assert.That(result.AddedContainers, Is.EqualTo(1));
            Assert.That(result.AddedItems, Is.EqualTo(1));
            Assert.That(result.AddedRelations, Is.EqualTo(1));
            Assert.That(result.AddedImages, Is.EqualTo(1));
        });

        Assert.That((await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All))).Select(c => c.ContainerId), Does.Contain(restoredContainerId));
        Assert.That((await items.QueryWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))).Select(i => i.ItemId), Does.Contain(restoredItemId));

        Assert.That(await store.TryRollbackLastCommitAsync(), Is.True);

        var containersAfterRollback = await containers.QueryAsync(new ContainerListSpecification(ContainerQueryFilter.All));
        var itemsAfterRollback = await items.QueryWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All));

        Assert.Multiple(() =>
        {
            Assert.That(containersAfterRollback.Select(c => c.ContainerId), Is.EquivalentTo(new[] { originalContainerId }));
            Assert.That(itemsAfterRollback, Is.Empty);
        });

    }
}
