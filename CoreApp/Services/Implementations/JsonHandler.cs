using System;
using System.Text.Json;

namespace CoreApp.Services.Implementations;

public static class JsonHandler
{
    public static void SerializeToFile<T>(string filePath, T data)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        var json = JsonSerializer.Serialize(data);
        File.WriteAllText(filePath, json);
    }

    public static T DeserializeFromFile<T>(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var json = File.ReadAllText(filePath);
        var result = JsonSerializer.Deserialize<T>(json);

        return result;
    }
}
