namespace CoreApp.Utilities;

public record class Constants
{
    public static readonly string PathToPhotos = Path.Combine(photoFolder, "Items");
    public static readonly string PathToContainers = Path.Combine(containerFolder, "Containers");

    private const string photoFolder = "Photos";
    private const string containerFolder = "Containers";

}
