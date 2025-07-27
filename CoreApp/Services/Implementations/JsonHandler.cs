using System;
using System.Text.Json;
using CoreApp.Services.Interfaces;

namespace CoreApp.Services.Implementations;

public class JsonHandler
{
    private readonly ITextFileHandler fileHandler;

    public JsonHandler(ITextFileHandler fileHandler)
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

    public static async Task<T> DeserializeFromFile<T>(string fileName, string folderName, string appDataPath)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentNullException(nameof(fileName));

        string filePath = Path.Combine(appDataPath, folderName, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        var result = JsonSerializer.Deserialize<T>(json);

        return result;
    }
}
