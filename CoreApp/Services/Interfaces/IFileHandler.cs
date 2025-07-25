using System;

namespace CoreApp.Services.Interfaces;

public interface IFileHandler
{
    Task<string> SaveFileAsync(string fileName, string folderName, byte[] data);
    Task<byte[]> ReadFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
}
