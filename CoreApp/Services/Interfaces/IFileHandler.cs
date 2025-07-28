using System;

namespace CoreApp.Services.Interfaces;

public interface IFileHandler
{
    Task<string> SaveFileAsync(string fileName, string folderPath, byte[] data);
    Task<byte[]> ReadFileAsync(string fileName, string folderPath);
    Task DeleteFileAsync(string filePath);
    string GetAppDataPath();
}
