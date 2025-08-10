using System;
using CoreApp.Services.Interfaces;

namespace MothballMobile.Core.Services;

public interface IMobileFileHandler : IFileHandler
{
    Task<ImageSource> GetImageSourceAsync(string fileName, string folderPath);
}
