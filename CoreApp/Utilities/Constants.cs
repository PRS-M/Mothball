namespace CoreApp.Utilities;

public record class Constants
{
    // Root data folder for app-specific files
    public const string DataFolder = "MothballData";
    public static readonly string PathToData = DataFolder; // placed under IFileHandler.GetAppDataPath()

    // Aggregate root file name
    public const string InventoryFileName = "inventory.json";

    // Legacy/auxiliary folders
    public static readonly string PathToItemPhotos = Path.Combine(PathToData, "Photos", "Items");
    public static readonly string PathToContainerPhotos = Path.Combine(PathToData, "Containers");

}
