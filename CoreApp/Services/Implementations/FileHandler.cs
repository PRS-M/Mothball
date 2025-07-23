using System;

namespace CoreApp.Services.Implementations;

public class FileHandler
{
    public void SaveFile(string filePath, byte[] data)
    {
        File.WriteAllBytes(filePath, data);
    }

    public byte[] ReadFile(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }

    public void DeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
