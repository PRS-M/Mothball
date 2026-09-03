using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Inventory;
using Infrastructure.Services.JsonStore;
using Infrastructure.Services.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mothball.Tests.Integration.Infrastructure.Persistence;

[TestFixture]
public sealed class CanonicalInventoryParityTests
{
    [Test]
    public async Task SQLiteAndJson_ProduceTheSameCanonicalBalances()
    {
        var workspace = new InventoryWorkspaceId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var item = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var unassigned = new InventoryPlacementId(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var container = new InventoryPlacementId(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var sqlitePath = Path.Combine(Path.GetTempPath(), $"mothball-canonical-{Guid.NewGuid():N}.db");
        await using var database = new MothballDatabase(sqlitePath);
        var sqlite = new SqliteCanonicalInventoryRepository(database);
        var json = new JsonCanonicalInventoryRepository(new JsonInventoryStore(new MemoryFileHandler(), NullLogger<JsonInventoryStore>.Instance));
        var jsonStore = (JsonCanonicalInventoryRepository)json;

        var sqliteBalances = await RunSequenceAsync(sqlite, workspace, item, unassigned, container);
        var jsonBalances = await RunSequenceAsync(jsonStore, workspace, item, unassigned, container);

        Assert.That(jsonBalances, Is.EqualTo(sqliteBalances));
        File.Delete(sqlitePath);
    }

    private static async Task<Dictionary<Guid, int>> RunSequenceAsync(ICanonicalInventoryRepository repository, InventoryWorkspaceId workspace, Guid item, InventoryPlacementId unassigned, InventoryPlacementId container)
    {
        var opening = new InventoryBalance(workspace, item, unassigned, 0);
        var receipt = InventoryMovementPlanner.PlanReceipt(opening, 10, "Receipt", DateTimeOffset.UtcNow, Guid.Parse("11111111-1111-1111-1111-111111111111"));
        await repository.ApplyAsync(receipt);
        var placed = InventoryMovementPlanner.PlanTransfer(receipt.ResultingBalances[0], new InventoryBalance(workspace, item, container, 0), 4, "Place", DateTimeOffset.UtcNow, Guid.Parse("22222222-2222-2222-2222-222222222222"));
        await repository.ApplyAsync(placed);
        var withdrawn = InventoryMovementPlanner.PlanWithdrawal(placed.ResultingBalances[1], 1, "Withdraw", DateTimeOffset.UtcNow, Guid.Parse("33333333-3333-3333-3333-333333333333"));
        await repository.ApplyAsync(withdrawn);
        var adjusted = InventoryMovementPlanner.PlanAdjustment(placed.ResultingBalances[0], -1, "Count", DateTimeOffset.UtcNow, Guid.Parse("44444444-4444-4444-4444-444444444444"));
        await repository.ApplyAsync(adjusted);
        await repository.ApplyAsync(adjusted);
        return (await repository.GetBalancesAsync(workspace, item)).ToDictionary(x => x.PlacementId.Value, x => x.OnHandQuantity);
    }

    private sealed class MemoryFileHandler : IFileHandler
    {
        private readonly Dictionary<(string Folder, string File), string> files = [];
        public string AppDataPath => "/memory";
        public Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data) => throw new NotSupportedException();
        public Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath) => throw new NotSupportedException();
        public Task<byte[]> ReadFileAsync(string fileName, string folderPath) => throw new NotSupportedException();
        public Task DeleteFileAsync(string fileName, string folderPath) { files.Remove((folderPath, fileName)); return Task.CompletedTask; }
        public Task<string> SaveTextFileAsync(string fileName, string folderPath, string content) { files[(folderPath, fileName)] = content; return Task.FromResult($"/memory/{folderPath}/{fileName}"); }
        public Task<string> ReadTextFileAsync(string fileName, string folderPath) => Task.FromResult(files[(folderPath, fileName)]);
        public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*") => files.Keys.Where(x => x.Folder == folderPath).Select(x => x.File).ToList();
    }
}
