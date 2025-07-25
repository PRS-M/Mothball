using System;

namespace CoreApp.Services.Interfaces;

public interface ITextFileHandler
{
    Task<string> SaveTextFileAsync(string fileName, string folderName, string content);
    Task<string> ReadTextFileAsync(string filePath);
}
