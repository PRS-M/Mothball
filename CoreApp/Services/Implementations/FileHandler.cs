using System;
using CoreApp.Services.Interfaces;

namespace CoreApp.Services.Implementations;

public class FileHandler : IFileHandler
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

    public async Task<byte[]> ReadFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        return await File.ReadAllBytesAsync(fullPath);
    }

    public async Task DeleteFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        await Task.Run(() => File.Delete(fullPath));
    }

    public async Task<string> SaveTextFileAsync(string fileName, string folderPath, string content)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        await File.WriteAllTextAsync(fullPath, content);

        return fullPath;
    }

    public async Task<string> ReadTextFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        return await File.ReadAllTextAsync(fullPath);
    }

    public async Task<MemoryStream> GetImageMemoryStream(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        using FileStream stream = File.OpenRead(fullPath);
        byte[] imageBytes = new byte[stream.Length];
        int totalBytesRead = 0;

        while (stream.Position < stream.Length)
        {
            int bytesRead = await stream.ReadAsync(imageBytes, totalBytesRead, (int)(stream.Length - totalBytesRead));

            if (bytesRead == 0)
                break; // End of stream reached

            totalBytesRead += bytesRead;
        }

        return new MemoryStream(imageBytes);
    }

    protected string GetFullPath(string fileName, string folderName)
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
