using System;
using CoreApp.Services.Implementations;
using Microsoft.Maui.Storage;

namespace MothballMobile.Core.Services;

public class MobileFileSystemHandler : FileHandler
{
    private readonly IFileSystem fileSystem;

    public MobileFileSystemHandler(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    }

    public override string GetAppDataPath()
    {
        return fileSystem.AppDataDirectory;
    }
}
