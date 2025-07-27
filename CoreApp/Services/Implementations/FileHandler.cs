using System;
using CoreApp.Services.Interfaces;

namespace CoreApp.Services.Implementations;

public class FileHandler : IFileHandler, ITextFileHandler
{
    public async Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        await File.WriteAllBytesAsync(fullPath, data);

        return fullPath;
    }

    public virtual string GetAppDataPath()
    {
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    public async Task<byte[]> ReadFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task DeleteFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        await Task.Run(() => File.Delete(filePath));
    }

    public async Task<string> SaveTextFileAsync(string fileName, string folderName, string content)
    {
        string fullPath = GetFullPath(fileName, folderName);
        await File.WriteAllTextAsync(fullPath, content);

        return fullPath;
    }

    public async Task<string> ReadTextFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        return await File.ReadAllTextAsync(filePath);
    }

    private string GetFullPath(string fileName, string folderName)
    {
        string directoryPath = Path.Combine(GetAppDataPath(), folderName);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string fullPath = Path.Combine(directoryPath, fileName);
        return fullPath;
    }
}
