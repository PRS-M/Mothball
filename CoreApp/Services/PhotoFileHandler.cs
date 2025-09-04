using System;
using System.Threading.Tasks;
using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services;

public class PhotoFileHandler
{
    private readonly IFileHandler fileHandler;

    public PhotoFileHandler(IFileHandler fileHandler)
    {
        this.fileHandler = fileHandler;
    }

    // public async Task LoadPhotos(Container container)
    // {
    //     ArgumentNullException.ThrowIfNull(container);
    //     List<string> itemIds = container.Photos.Select(i => i.FileName).ToList();

    //     foreach (var itemId in itemIds)
    //     {
    //         if (itemId == 0)
    //             continue;

    //         StoredItem item = container.Items[itemId];
    //         foreach (var photo in item.Photos)
    //         {
    //             byte[] bytes = await fileHandler.ReadFileAsync(
    //                 photo.FileName,
    //                 Path.Combine(Constants.PathToItemPhotos, container.ContainerId));

    //             photo.ImageData = bytes;
    //         }
    //     }
    // }

    public ImageItem LoadPhoto(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentNullException("File path cannot be null.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("The specified file does not exist.", filePath);
        }

        byte[] imageData = File.ReadAllBytes(filePath);
        var fileInfo = new FileInfo(filePath);

        return new ImageItem
        {

        };
    }

    public List<ImageItem> LoadAllPhotos(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException("Directory path cannot be null.");
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException("The specified directory does not exist.");
        }

        var photos = new List<ImageItem>();
        var files = Directory.GetFiles(directoryPath, "*.jpg"); // Assuming photos are in JPG format

        foreach (var file in files)
        {
            var photo = LoadPhoto(file);
            if (photo != null)
            {
                photos.Add(photo);
            }
        }

        return photos;
    }
}
