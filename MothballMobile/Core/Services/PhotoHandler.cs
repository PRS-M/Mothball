using System;
using CoreApp;

namespace MothballMobile.Core.Services;

public class PhotoHandler
{
    public List<Photo> GetPhotosForContainer(Container container)
    {
        if (container == null || container.Items == null)
        {
            return new List<Photo>();
        }

        var photos = new List<Photo>();
        foreach (var item in container.Items)
        {
            if (item.Photos != null)
            {
                photos.AddRange(item.Photos);
            }
        }

        return photos;
    }
}
