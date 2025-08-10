using System;
using CoreApp.Services.Implementations;
using CoreApp.Services.Interfaces;
using Microsoft.Maui.Storage;

namespace MothballMobile.Core.Services;

public class MobileFileHandler : FileHandler, IMobileFileHandler
{
    private readonly IFileSystem fileSystem;

    public MobileFileHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public async Task<ImageSource> GetImageSourceAsync(string fileName, string folderPath)
    {
        MemoryStream memoryStream = await GetImageMemoryStream(fileName, folderPath);
        return ImageSource.FromStream(() => memoryStream);
    }

    public static ImageSource GetImageMemoryStream(MemoryStream memoryStream)
    {
        if (memoryStream == null)
        {
            throw new ArgumentNullException(nameof(memoryStream));
        }

        memoryStream.Position = 0; // Reset stream position
        return ImageSource.FromStream(() => memoryStream);
    }

    public override string GetAppDataPath()
    {
        return fileSystem.AppDataDirectory;
    }
}
