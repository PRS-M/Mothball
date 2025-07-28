using System;

namespace CoreApp.Services.Implementations;

public class PhotoLoader
{
    public void LoadPhotoDataForContainer(Container container)
    {
        if (container == null || container.Items == null)
        {
            return;
        }

        foreach (var item in container.Items)
        {
            foreach (var photo in item.Photos)
            {
                // Load photo data for each photo
                var photoData = LoadPhotoData(photo);
                if (photoData != null)
                {
                    item.PhotosWithData.Add(photoData);
                }
            }
        }
    }

    private Photo LoadPhotoData(Photo photo)
    {
        ArgumentNullException.ThrowIfNull(photo);

        return new Photo
        {
            FileName = photo.FileName,
            DateTaken = photo.DateTaken,
            ImageData = LoadImageData(photo.FileName)
        };
    }

    private byte[] LoadImageData(string fileName)
    {
        // Simulate loading image data
        return File.ReadAllBytes(fileName);
    }
}
