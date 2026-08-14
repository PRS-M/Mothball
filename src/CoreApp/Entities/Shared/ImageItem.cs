namespace CoreApp.Entities.Shared;

public record ImageItem // Value Object
{
    public ImageItem()
    {
        ImageId = Guid.NewGuid();
    }

    public ImageItem(Guid imageId)
    {
        ImageId = imageId;
    }

    public Guid ImageId { get; }
    public string FileName => $"{ImageId}.jpg";
}
