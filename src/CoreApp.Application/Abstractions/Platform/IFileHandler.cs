namespace CoreApp.Application.Abstractions.Platform;

/// <summary>
/// Provides platform-agnostic file system operations for the application.
/// Abstracts file I/O operations to enable testing and platform independence.
/// </summary>
public interface IFileHandler
{
    /// <summary>
    /// Gets the platform-specific app data directory path.
    /// This is the root directory for all application file operations.
    /// </summary>
    /// <returns>The full path to the app data directory.</returns>
    string AppDataPath { get; }

    /// <summary>
    /// Saves binary data to a file in the specified folder.
    /// Creates the folder structure if it doesn't exist.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="folderPath">The relative folder path within the app data directory.</param>
    /// <param name="data">The binary data to write to the file.</param>
    /// <returns>The full path to the saved file.</returns>
    Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data);

    /// <summary>
    /// Copies a file from the application package (raw resources) to the app data directory.
    /// Useful for extracting bundled assets to writable storage.
    /// </summary>
    /// <param name="rawFileName">The name of the file in the application package.</param>
    /// <param name="destFileName">The destination file name.</param>
    /// <param name="destFolderPath">The destination folder path within the app data directory.</param>
    Task CopyFileFromRawToAppDataAsync(string rawFileName, string destFileName, string destFolderPath);

    /// <summary>
    /// Reads the entire contents of a file as binary data.
    /// </summary>
    /// <param name="fileName">The name of the file to read.</param>
    /// <param name="folderPath">The folder path within the app data directory.</param>
    /// <returns>The file contents as a byte array.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    Task<byte[]> ReadFileAsync(string fileName, string folderPath);

    /// <summary>
    /// Deletes a file from the file system.
    /// </summary>
    /// <param name="fileName">The name of the file to delete.</param>
    /// <param name="folderPath">The folder path within the app data directory.</param>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    Task DeleteFileAsync(string fileName, string folderPath);

    /// <summary>
    /// Saves text content to a file in the specified folder.
    /// Creates the folder structure if it doesn't exist.
    /// </summary>
    /// <param name="fileName">The name of the file to create.</param>
    /// <param name="folderPath">The folder path within the app data directory.</param>
    /// <param name="content">The text content to write to the file.</param>
    /// <returns>The full path to the saved file.</returns>
    Task<string> SaveTextFileAsync(string fileName, string folderPath, string content);

    /// <summary>
    /// Reads the entire contents of a text file.
    /// </summary>
    /// <param name="fileName">The name of the file to read.</param>
    /// <param name="folderPath">The folder path within the app data directory.</param>
    /// <returns>The file contents as a string.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    Task<string> ReadTextFileAsync(string fileName, string folderPath);

    /// <summary>
    /// Enumerates files in a directory that match the specified search pattern.
    /// Returns an empty collection if the directory doesn't exist.
    /// </summary>
    /// <param name="folderPath">The folder path within the app data directory.</param>
    /// <param name="searchPattern">The search pattern to match files (default: "*.*").</param>
    /// <returns>An enumerable of file names (without path) that match the pattern.</returns>
    IEnumerable<string> EnumerateFiles(string folderPath, string searchPattern = "*.*");
}
