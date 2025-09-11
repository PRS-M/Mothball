using CoreApp.Interfaces;
using CoreApp.Utilities;
using System.IO;

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
    public string AppDataPath => fileSystem.AppDataDirectory;

    /// <inheritdoc />
    public async Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data)
    {
        string fullPath = GetWriteFullPath(fileName, folderPath);
        await File.WriteAllBytesAsync(fullPath, data).ConfigureAwait(false);
        return fullPath;
    }

    /// <inheritdoc />
    public async Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath)
    {
        using Stream input = await FileSystem.OpenAppPackageFileAsync(rawFileName);
        string destFullPath = GetWriteFullPath(destFileName, destFolderPath);

        using FileStream output = File.Create(destFullPath);
        await input.CopyToAsync(output).ConfigureAwait(false);
    }

    public async Task CopyFileAsync(string sourceFileName, string sourceFolderFullPath, string destFileName, string destFolderPath)
    {
        // resolve source without creating directories
        string sourceFullPath = GetFullPath(sourceFileName, sourceFolderFullPath);
        string destFullPath = GetWriteFullPath(destFileName, destFolderPath);

        ThrowIfFileNotExists(sourceFullPath);

        await Task.Run(() => File.Copy(sourceFullPath, destFullPath, true)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        ThrowIfFileNotExists(fullPath);

        return await File.ReadAllBytesAsync(fullPath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        ThrowIfFileNotExists(fullPath);

        await Task.Run(() => File.Delete(fullPath)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> SaveTextFileAsync(string fileName, string folderPath, string content)
    {
        string fullPath = GetWriteFullPath(fileName, folderPath);
        await File.WriteAllTextAsync(fullPath, content).ConfigureAwait(false);
        return fullPath;
    }

    /// <inheritdoc />
    public async Task<string> ReadTextFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        ThrowIfFileNotExists(fullPath);

        return await File.ReadAllTextAsync(fullPath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*")
    {
        string directoryPath = GetDirectoryPath(folderPath);
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

    private static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    private static void ThrowIfFileNotExists(string fullPath)
    {
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {fullPath}");
    }

    // Helper: returns the directory path under appdata (does not create it)
    private string GetDirectoryPath(string folderPath)
    {
        return Path.Combine(AppDataPath, folderPath ?? string.Empty);
    }

    // Helper: returns the full file path (does not create directory)
    private string GetFullPath(string fileName, string folderPath)
    {
        return Path.Combine(GetDirectoryPath(folderPath), fileName);
    }

    // Helper: ensures directory exists and returns full path for writing
    private string GetWriteFullPath(string fileName, string folderPath)
    {
        var dir = GetDirectoryPath(folderPath);
        EnsureDirectoryExists(dir);
        return Path.Combine(dir, fileName);
    }
}
