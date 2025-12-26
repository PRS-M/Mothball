using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Utilities;

namespace UnitTests;

[TestFixture]
public class InventoryJsonHandlerTests
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
    public async Task SaveAsync_List_RoundTrips_WithLoadAsync()
    {
        var files = new InMemoryFileHandler();
        var json = new JsonHandler(files);
        var inv = new InventoryJsonHandler(json);

        var c1 = new Container(Guid.NewGuid(), "Box", "Notes");
        var c2 = new Container(Guid.NewGuid(), "Crate", "More");

        await inv.SaveAsync(new List<Container> { c1, c2 });

        var loaded = await inv.LoadAsync();

        Assert.That(loaded.Select(c => c.ContainerId), Is.EquivalentTo(new[] { c1.ContainerId, c2.ContainerId }));
        Assert.That(loaded.Single(c => c.ContainerId == c1.ContainerId).Name, Is.EqualTo("Box"));
        Assert.That(loaded.Single(c => c.ContainerId == c2.ContainerId).Notes, Is.EqualTo("More"));
    }

    [Test]
    public async Task SaveAsync_Single_WritesArrayShape_CompatibleWithLoadAsync()
    {
        var files = new InMemoryFileHandler();
        var json = new JsonHandler(files);
        var inv = new InventoryJsonHandler(json);

        var c = new Container(Guid.NewGuid(), "Box", "Notes");

        await inv.SaveAsync(c);

        var raw = await files.ReadTextFileAsync(Constants.InventoryFileName, Constants.PathToData);
        Assert.That(raw.TrimStart().StartsWith('['), Is.True);

        var loaded = await inv.LoadAsync();
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded[0].ContainerId, Is.EqualTo(c.ContainerId));
    }
}
