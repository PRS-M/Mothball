namespace CoreApp.Entities.Shared;

public record Photo
{
    public Photo()
    {
        PhotoId = Guid.NewGuid();
    }

    public Photo(Guid photoId)
    {
        PhotoId = photoId;
    }

    public Guid PhotoId { get; set; }
    public string FileName => $"{PhotoId}.jpg";
}
