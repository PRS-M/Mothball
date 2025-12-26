using System.Text.Json;
using CoreApp.Services;
using CoreApp.Interfaces;

namespace UnitTests;

[TestFixture]
public class JsonHandlerBehaviorTests
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
        {
            // Minimal implementation for tests: return file names in that folder.
            // Pattern matching is intentionally not implemented here.
            return textFiles.Keys
                .Where(k => k.folder == folderPath)
                .Select(k => k.file)
                .Distinct()
                .ToList();
        }
    }

    [Test]
    public async Task SerializeToFile_WritesSerializedJson_ToFileHandler()
    {
        var files = new InMemoryFileHandler();
        var handler = new JsonHandler(files);

        var payload = new { A = 1, B = "two" };
        var expected = JsonSerializer.Serialize(payload);

        await handler.SerializeToFile("x.json", "folder", payload);

        var actual = await files.ReadTextFileAsync("x.json", "folder");
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void SerializeToFile_Throws_OnDefaultData()
    {
        var files = new InMemoryFileHandler();
        var handler = new JsonHandler(files);

        Assert.ThrowsAsync<ArgumentNullException>(() => handler.SerializeToFile<string>("x.json", "folder", null!));
    }

    [Test]
    public void DeserializeFromFile_Throws_OnNullFolder()
    {
        var files = new InMemoryFileHandler();
        var handler = new JsonHandler(files);

        Assert.ThrowsAsync<ArgumentNullException>(() => handler.DeserializeFromFile<object>("x.json", null!));
    }

    [Test]
    public void DeserializeFromFile_ThrowsJsonException_OnInvalidJson()
    {
        var files = new InMemoryFileHandler();
        var handler = new JsonHandler(files);

        Assert.DoesNotThrowAsync(async () => await files.SaveTextFileAsync("bad.json", "folder", "not-json"));

        Assert.ThrowsAsync<JsonException>(() => handler.DeserializeFromFile<Dictionary<string, int>>("bad.json", "folder"));
    }

    [Test]
    public void EnumerateJsonFiles_DelegatesToFileHandler()
    {
        var files = new InMemoryFileHandler();
        var handler = new JsonHandler(files);

        Assert.DoesNotThrowAsync(async () =>
        {
            await files.SaveTextFileAsync("a.json", "folder", "{}");
            await files.SaveTextFileAsync("b.json", "folder", "{}");
        });

        var enumerated = handler.EnumerateJsonFiles("folder").ToList();
        Assert.That(enumerated, Is.EquivalentTo(new[] { "a.json", "b.json" }));
    }
}
