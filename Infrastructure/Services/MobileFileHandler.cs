using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace Infrastructure.Services;

public class MobileFileHandler : IFileHandler
{
    private readonly IFileSystem fileSystem;

    public MobileFileHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public string GetAppDataPath() => fileSystem.AppDataDirectory;

    public async Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        await File.WriteAllBytesAsync(fullPath, data);
        return fullPath;
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
            if (bytesRead == 0) break;
            totalBytesRead += bytesRead;
        }

        return new MemoryStream(imageBytes);
    }

    public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*")
    {
        string directoryPath = Path.Combine(GetAppDataPath(), folderPath);
        if (!Directory.Exists(directoryPath))
        {
            return Enumerable.Empty<string>();
        }

        var files = Directory.EnumerateFiles(directoryPath, searchPattern)
            .Select(Path.GetFileName)!
            .Where(n => !string.IsNullOrEmpty(n))
            .Cast<string>();

        return files;
    }

    private string GetFullPath(string fileName, string folderPath)
    {
        string fullDirectoryPath = Path.Combine(GetAppDataPath(), Constants.PathToData, folderPath);
        if (!Directory.Exists(fullDirectoryPath))
        {
            Directory.CreateDirectory(fullDirectoryPath);
        }

        string fullPath = Path.Combine(fullDirectoryPath, fileName);
        return fullPath;
    }
}
