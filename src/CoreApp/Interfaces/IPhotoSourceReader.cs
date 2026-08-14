using CoreApp.Utilities;

namespace CoreApp.Interfaces;

public interface IPhotoSourceReader
{
    Task<byte[]> GetPhotoBytesAsync(PhotoSource source, IProgress<double>? resizeProgress);
}
