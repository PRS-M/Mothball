using System;
using System.Threading.Tasks;
using CoreApp.Services.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Services;

public class PhotoFileHandler
{
    private readonly IFileHandler fileHandler;

    public PhotoFileHandler(IFileHandler fileHandler)
    {
        this.fileHandler = fileHandler;
    }

    public async Task LoadPhotos(Container container)
    {
        ArgumentNullException.ThrowIfNull(container);

        container.Photo = new Photo
        {
            FileName = $"{container.Name}-photo.jpg",
        };
        foreach (var item in container.Items)
        {
            foreach (var photo in item.Photos)
            {
                byte[] bytes = await fileHandler.ReadFileAsync(photo.FileName, Constants.PathToPhotos);
                photo.ImageData = bytes;
            }
        }
    }

    public Photo LoadPhoto(string filePath)
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
        DateTime dateTaken = fileInfo.CreationTime;

        return new Photo
        {
            FileName = Guid.NewGuid().ToString(),
            // ImageData = imageData,
            DateTaken = dateTaken
        };
    }

    public List<Photo> LoadAllPhotos(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath))
        {
            throw new ArgumentNullException("Directory path cannot be null.");
        }

        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException("The specified directory does not exist.");
        }

        var photos = new List<Photo>();
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
