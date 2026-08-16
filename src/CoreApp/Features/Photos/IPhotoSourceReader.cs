using CoreApp.Interfaces;
using CoreApp.Utilities;

namespace CoreApp.Features.Photos;

public interface IPhotoSourceReader
{
    Task<byte[]> GetPhotoBytesAsync(PhotoSource source, IProgress<double>? resizeProgress);
}
