namespace CoreApp.Domain.ValueObjects;

/// <summary>
/// Identifies an image stored for an inventory entity.
/// </summary>
public record ImageItem
{
    public ImageItem()
    {
        ImageId = Guid.NewGuid();
    }

    public ImageItem(Guid imageId)
    {
        if (imageId == Guid.Empty)
        {
            throw new ArgumentException("Image ID cannot be empty.", nameof(imageId));
        }

        ImageId = imageId;
    }

    public Guid ImageId { get; }
    public string FileName => $"{ImageId}.jpg";
}
