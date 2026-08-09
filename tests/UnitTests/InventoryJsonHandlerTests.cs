using CoreApp.Entities.ContainerAggregate;
using CoreApp.Interfaces;
using CoreApp.Services;
using CoreApp.Utilities;
using Moq;

namespace UnitTests;

[TestFixture]
public class InventoryJsonHandlerTests
{
    private static Mock<IFileHandler> CreateFileHandler(out Dictionary<(string folder, string file), string> textFiles)
    {
        var store = new Dictionary<(string folder, string file), string>();
        textFiles = store;
        var fileHandler = new Mock<IFileHandler>(MockBehavior.Strict);
        fileHandler.SetupGet(f => f.AppDataPath).Returns("/appdata");
        fileHandler.Setup(f => f.SaveTextFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string, string>((fileName, folderPath, content) =>
            {
                store[(folderPath, fileName)] = content;
                return Task.FromResult($"/appdata/{folderPath}/{fileName}");
            });
        fileHandler.Setup(f => f.ReadTextFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((fileName, folderPath) =>
            {
                if (!store.TryGetValue((folderPath, fileName), out var content))
                {
                    return Task.FromException<string>(new FileNotFoundException($"Missing file: {folderPath}/{fileName}"));
                }

                return Task.FromResult(content);
            });
        fileHandler.Setup(f => f.DeleteFileAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((fileName, folderPath) =>
            {
                store.Remove((folderPath, fileName));
                return Task.CompletedTask;
            });
        fileHandler.Setup(f => f.EnumerateFiles(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((folderPath, _) =>
                store.Keys.Where(k => k.folder == folderPath).Select(k => k.file).Distinct().ToList());

        return fileHandler;
    }

    [Test]
    public async Task SaveAsync_List_RoundTrips_WithLoadAsync()
    {
        var files = CreateFileHandler(out _);
        var json = new JsonHandler(files.Object);
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
        var files = CreateFileHandler(out _);
        var json = new JsonHandler(files.Object);
        var inv = new InventoryJsonHandler(json);

        var c = new Container(Guid.NewGuid(), "Box", "Notes");

        await inv.SaveAsync(c);

        var raw = await files.Object.ReadTextFileAsync(Constants.InventoryFileName, Constants.PathToData);
        Assert.That(raw.TrimStart().StartsWith('['), Is.True);

        var loaded = await inv.LoadAsync();
        Assert.That(loaded.Count, Is.EqualTo(1));
        Assert.That(loaded[0].ContainerId, Is.EqualTo(c.ContainerId));
    }
}
