using System;
using System.Collections;
using System.Text.Json;
using CoreApp.Services.Interfaces;

namespace CoreApp.Services.Implementations;

public class JsonHandler
{
    private readonly IFileHandler fileHandler;

    public JsonHandler(IFileHandler fileHandler)
    {
        this.fileHandler = fileHandler;
    }

    public async Task<string> SerializeToFile<T>(string fileName, string folderName, T data)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (EqualityComparer<T>.Default.Equals(data, default))
            throw new ArgumentNullException(nameof(data));

        string json = JsonSerializer.Serialize(data);
        return await fileHandler.SaveTextFileAsync(fileName, folderName, json);
    }

    public IEnumerable<string> EnumerateJsonFiles(string folderPath)
    {
        foreach (string file in fileHandler.EnumerateFiles(folderPath, "*.json"))
        {
            yield return file;
        }
    }

    public async Task<T> DeserializeFromFile<T>(string fileName, string folderName)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        if (string.IsNullOrEmpty(folderName))
            throw new ArgumentNullException(nameof(folderName));

        var json = await fileHandler.ReadTextFileAsync(fileName, folderName);
        var result = JsonSerializer.Deserialize<T>(json);

        return result ?? throw new JsonException($"Failed to deserialize JSON from file: {fileName}");
    }
}
