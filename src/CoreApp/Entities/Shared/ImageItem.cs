namespace CoreApp.Entities.Shared;

public record ImageItem // Value Object
{
    public ImageItem()
    {
        ImageId = Guid.NewGuid();
    }

    public ImageItem(Guid photoId)
    {
        ImageId = photoId;
    }

    public Guid ImageId { get; }
    public string FileName => $"{ImageId}.jpg";
}
