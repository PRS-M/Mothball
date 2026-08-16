using CoreApp.Utilities;
using System.IO;

namespace Infrastructure.Services;

/// <summary>
/// Mobile platform implementation of file handling operations using MAUI's IFileSystem.
/// </summary>
public class MobileFileHandler : IFileHandler
{
    private readonly IFileSystem fileSystem;
    private readonly string appDataRootPath;

    public MobileFileHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        appDataRootPath = Path.GetFullPath(this.fileSystem.AppDataDirectory);
    }

    /// <inheritdoc />
    public string AppDataPath => appDataRootPath;

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

        const int bufferSize = 81920;
        await using var source = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        await using var destination = new FileStream(destFullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
        await source.CopyToAsync(destination).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        ThrowIfFileNotExists(fullPath);

        return await File.ReadAllBytesAsync(fullPath).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string fileName, string folderPath)
    {
        string fullPath = GetFullPath(fileName, folderPath);
        ThrowIfFileNotExists(fullPath);

        File.Delete(fullPath);
        return Task.CompletedTask;
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

    // Helper: returns the validated directory path under app data (does not create it)
    private string GetDirectoryPath(string folderPath)
    {
        var normalizedFolderPath = folderPath ?? string.Empty;
        var candidate = Path.IsPathRooted(normalizedFolderPath)
            ? Path.GetFullPath(normalizedFolderPath)
            : Path.GetFullPath(Path.Combine(AppDataPath, normalizedFolderPath));

        EnsurePathUnderAppData(candidate);
        return candidate;
    }

    // Helper: returns the validated full file path (does not create directory)
    private string GetFullPath(string fileName, string folderPath)
    {
        return GetValidatedFilePath(fileName, folderPath, ensureDirectory: false);
    }

    // Helper: ensures directory exists and returns full path for writing
    private string GetWriteFullPath(string fileName, string folderPath)
    {
        return GetValidatedFilePath(fileName, folderPath, ensureDirectory: true);
    }

    private string GetValidatedFilePath(string fileName, string folderPath, bool ensureDirectory)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));

        var directoryPath = GetDirectoryPath(folderPath);
        if (ensureDirectory)
        {
            EnsureDirectoryExists(directoryPath);
        }

        var fullPath = Path.GetFullPath(Path.Combine(directoryPath, fileName));
        EnsurePathUnderAppData(fullPath);
        return fullPath;
    }

    private void EnsurePathUnderAppData(string candidatePath)
    {
        var fullCandidate = Path.GetFullPath(candidatePath);
        var normalizedRoot = appDataRootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedCandidate = fullCandidate.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        var isRoot = string.Equals(normalizedCandidate, normalizedRoot, comparison);
        var isChild = normalizedCandidate.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, comparison)
                      || normalizedCandidate.StartsWith(normalizedRoot + Path.AltDirectorySeparatorChar, comparison);

        if (!isRoot && !isChild)
        {
            throw new UnauthorizedAccessException($"Path '{candidatePath}' is outside the app data directory.");
        }
    }
}
