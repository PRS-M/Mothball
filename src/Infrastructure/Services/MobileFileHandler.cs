using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace Infrastructure.Services;

/// <summary>
/// Mobile platform implementation of file handling operations using MAUI's IFileSystem.
/// </summary>
public class MobileFileHandler : IFileHandler
{
    private readonly IFileSystem fileSystem;

    public MobileFileHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    /// <inheritdoc />
    public string GetAppDataPath() => fileSystem.AppDataDirectory;

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        await File.WriteAllBytesAsync(fullPath, data);
        return fullPath;
    }

    /// <inheritdoc />
    public async Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath)
    {
        using Stream input = await FileSystem.OpenAppPackageFileAsync(rawFileName);
        string destFullPath = GetFullPath(destFileName, destFolderPath);

        using FileStream output = File.Create(destFullPath);
        await input.CopyToAsync(output);
    }

    public async Task CopyFileAsync(string sourceFileName, string sourceFolderFullPath, string destFileName, string destFolderPath)
    {
        string sourceFullPath = GetFullPath(sourceFileName, sourceFolderFullPath);
        string destFullPath = GetFullPath(destFileName, destFolderPath);

        if (!File.Exists(sourceFullPath))
            throw new FileNotFoundException($"File not found: {sourceFullPath}");

        await Task.Run(() => File.Copy(sourceFullPath, destFullPath, true));
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        return await File.ReadAllBytesAsync(fullPath);
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        await Task.Run(() => File.Delete(fullPath));
    }

    /// <inheritdoc />
    public async Task<string> SaveTextFileAsync(string fileName, string folderPath, string content)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        await File.WriteAllTextAsync(fullPath, content);
        return fullPath;
    }

    /// <inheritdoc />
    public async Task<string> ReadTextFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");

        return await File.ReadAllTextAsync(fullPath);
    }

    /// <inheritdoc />
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
        string fullDirectoryPath = Path.Combine(GetAppDataPath(), folderPath);
        if (!Directory.Exists(fullDirectoryPath))
        {
            Directory.CreateDirectory(fullDirectoryPath);
        }

        string fullPath = Path.Combine(fullDirectoryPath, fileName);
        return fullPath;
    }
}
