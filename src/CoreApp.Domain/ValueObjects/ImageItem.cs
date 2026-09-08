namespace CoreApp.Domain.ValueObjects;

/// <summary>
/// Identifies an image stored for an inventory entity.
/// </summary>
public record ImageItem
{
    public ImageItem() : this(Guid.NewGuid(), fileName: null, isSharedAsset: false)
    {
    }

    public ImageItem(Guid imageId)
        : this(imageId, fileName: null, isSharedAsset: false)
    {
    }

    /// <summary>
    /// Creates an image reference, optionally pointing to a shared application asset.
    /// </summary>
    /// <param name="imageId">The unique metadata identifier.</param>
    /// <param name="fileName">The stored filename override, when applicable.</param>
    /// <param name="isSharedAsset">Whether the file is shared by multiple image records.</param>
    public ImageItem(Guid imageId, string? fileName, bool isSharedAsset)
    {
        if (imageId == Guid.Empty)
        {
            throw new ArgumentException("Image ID cannot be empty.", nameof(imageId));
        }

        ImageId = imageId;
        FileName = string.IsNullOrWhiteSpace(fileName) ? $"{imageId}.jpg" : fileName;
        IsSharedAsset = isSharedAsset;
    }

    public Guid ImageId { get; }
    public string FileName { get; }
    public bool IsSharedAsset { get; }
}
