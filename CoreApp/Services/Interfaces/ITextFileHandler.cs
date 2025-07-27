using System;

namespace CoreApp.Services.Interfaces;

public interface ITextFileHandler
{
    Task<string> SaveTextFileAsync(string fileName, string folderPath, string content);
    Task<string> ReadTextFileAsync(string filePath);
}
