namespace CoreApp.Utilities;

public record class Constants
{
    // Root data folder for app-specific files
    public const string DataFolder = "MothballData";
    public static readonly string PathToData = DataFolder; // placed under IFileHandler.GetAppDataPath()

    // Aggregate root file name
    public const string InventoryFileName = "inventory.json";

    public static readonly string PathToItemPhotos = Path.Combine(PathToData, "Photos", "Items");
    public static readonly string PathToContainerPhotos = Path.Combine(PathToData, "Photos", "Containers");

    // Photo handling
    // Captured photos are stored as thumbnails to reduce storage usage.
    public const int PhotoThumbnailMaxWidthPx = 512;
    public const int PhotoThumbnailMaxHeightPx = 512;
}
