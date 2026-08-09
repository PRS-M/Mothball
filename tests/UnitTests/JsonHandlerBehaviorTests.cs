using System.Text.Json;
using CoreApp.Services;
using CoreApp.Interfaces;
using Moq;

namespace UnitTests;

[TestFixture]
public class JsonHandlerBehaviorTests
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
    public async Task SerializeToFile_WritesSerializedJson_ToFileHandler()
    {
        var files = CreateFileHandler(out _);
        var handler = new JsonHandler(files.Object);

        var payload = new { A = 1, B = "two" };
        var expected = JsonSerializer.Serialize(payload);

        await handler.SerializeToFile("x.json", "folder", payload);

        var actual = await files.Object.ReadTextFileAsync("x.json", "folder");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public async Task SerializeToFile_Throws_OnDefaultData()
    {
        var files = CreateFileHandler(out _);
        var handler = new JsonHandler(files.Object);

        Assert.ThrowsAsync<ArgumentNullException>(() => handler.SerializeToFile<string>("x.json", "folder", null!));
    }

    [Test]
    public async Task DeserializeFromFile_Throws_OnNullFolder()
    {
        var files = CreateFileHandler(out _);
        var handler = new JsonHandler(files.Object);

        Assert.ThrowsAsync<ArgumentNullException>(() => handler.DeserializeFromFile<object>("x.json", null!));
    }

    [Test]
    public async Task DeserializeFromFile_ThrowsJsonException_OnInvalidJson()
    {
        var files = CreateFileHandler(out _);
        var handler = new JsonHandler(files.Object);

        await files.Object.SaveTextFileAsync("bad.json", "folder", "not-json");

        Assert.ThrowsAsync<JsonException>(() => handler.DeserializeFromFile<Dictionary<string, int>>("bad.json", "folder"));
    }

    [Test]
    public async Task EnumerateJsonFiles_DelegatesToFileHandler()
    {
        var files = CreateFileHandler(out _);
        var handler = new JsonHandler(files.Object);

        await files.Object.SaveTextFileAsync("a.json", "folder", "{}");
        await files.Object.SaveTextFileAsync("b.json", "folder", "{}");

        var enumerated = handler.EnumerateJsonFiles("folder").ToList();
        Assert.That(enumerated, Is.EquivalentTo(new[] { "a.json", "b.json" }));
    }
}
