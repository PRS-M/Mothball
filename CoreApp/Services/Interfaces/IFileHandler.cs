using System;

namespace CoreApp.Services.Interfaces;

public interface IFileHandler
{
    Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data);
    Task<byte[]> ReadFileAsync(string fileName, string folderPath);
    Task DeleteFileAsync(string fileName, string folderPath);
    Task<string> SaveTextFileAsync(string fileName, string folderPath, string content);
    Task<string> ReadTextFileAsync(string fileName, string folderPath);
    Task<MemoryStream> GetImageMemoryStream(string fileName, string folderPath);
    Task<IEnumerable<string>> EnumerateFilesAsync(string folderPath, string searchPattern = "*.*");
    string GetAppDataPath();
}
