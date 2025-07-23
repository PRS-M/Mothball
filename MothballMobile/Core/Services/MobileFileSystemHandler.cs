using System;
using Microsoft.Maui.Storage;

namespace MothballMobile.Core.Services;

public class MobileFileSystemHandler
{
    private readonly IFileSystem fileSystem;

    public MobileFileSystemHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public string GetAppDataPath()
    {
        return fileSystem.AppDataDirectory;
    }

    public async Task SaveFileAsync(string fileName, string folderName, byte[] data)
    {
        string fullPath = Path.Combine(GetAppDataPath(), folderName, fileName);
        await File.WriteAllBytesAsync(fullPath, data);
    }
}
