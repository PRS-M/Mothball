using CoreApp.Entities.Shared;

namespace CoreApp.Interfaces;

public interface IPhotoFilePersistenceService
{
    Task<int> PersistPhotoBytesAsync(
        byte[] bytes,
        Func<ImageItem> addImageItem,
        Action<Guid> removeImageItem,
        string saveDirectory,
        Func<ImageItem, Task> persistAsync);
}
