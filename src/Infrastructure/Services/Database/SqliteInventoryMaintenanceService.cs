using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Abstractions.Platform;
using CoreApp.Application.Utilities;
using Infrastructure.Services.DatabaseModels;

namespace Infrastructure.Services.Database;

/// <summary>
/// Performs destructive maintenance operations against the SQLite operational store.
/// </summary>
public sealed class SqliteInventoryMaintenanceService : IInventoryMaintenanceService
{
    private const string SharedContainerPhoto = "seeded-container.jpg";
    private const string SharedItemPhoto = "seeded-item.jpg";
    private readonly MothballDatabase database;
    private readonly IFileHandler files;

    public SqliteInventoryMaintenanceService(MothballDatabase database, IFileHandler files)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null)
    {
        await database.InitializeAsync();
        var containers = await database.Connection.Table<DbContainer>().ToListAsync();
        var items = await database.Connection.Table<DbItem>().ToListAsync();
        var containerIds = containers.Select(container => container.ContainerId).ToHashSet();
        var itemIds = items.Select(item => item.ItemId).ToHashSet();
        var images = await database.Connection.Table<DbImage>().ToListAsync();

        await EnsureSharedAssetAsync("container.png", SharedContainerPhoto);
        await EnsureSharedAssetAsync("mothball_logo.png", SharedItemPhoto);

        for (var index = 0; index < images.Count; index++)
        {
            var image = images[index];
            image.StoredFileName = containerIds.Contains(image.OwnerUniqueId) ? SharedContainerPhoto :
                itemIds.Contains(image.OwnerUniqueId) ? SharedItemPhoto : image.StoredFileName;
            image.IsSharedAsset = containerIds.Contains(image.OwnerUniqueId) || itemIds.Contains(image.OwnerUniqueId);
            image.ImageData = null;
            await database.Connection.UpdateAsync(image);
            progress?.Report(new MaintenanceProgress((index + 1d) / Math.Max(images.Count, 1), "Replacing photos"));
        }

        await DeleteRegularPhotoFilesAsync(Constants.PathToContainerPhotos);
        await DeleteRegularPhotoFilesAsync(Constants.PathToItemPhotos);
        progress?.Report(new MaintenanceProgress(1, "Photos replaced"));
    }

    public async Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null)
    {
        progress?.Report(new MaintenanceProgress(0.1, "Deleting inventory data"));
        await database.ResetAsync();
        progress?.Report(new MaintenanceProgress(0.7, "Deleting photo files"));
        await DeleteAllFilesAsync(Constants.PathToContainerPhotos);
        await DeleteAllFilesAsync(Constants.PathToItemPhotos);
        await DeleteAllFilesAsync(Constants.PathToSharedPhotos);
        progress?.Report(new MaintenanceProgress(1, "Data reset complete"));
    }

    public Task<bool> TryRecoverAsync() => Task.FromResult(true);

    public Task<bool> TryRollbackLastCommitAsync() => Task.FromResult(false);

    private async Task EnsureSharedAssetAsync(string rawName, string storedName)
    {
        if (!files.FileExists(storedName, Constants.PathToSharedPhotos))
        {
            await files.CopyFileFromRawToAppDataAsync(rawName, storedName, Constants.PathToSharedPhotos);
        }
    }

    private async Task DeleteRegularPhotoFilesAsync(string folder)
    {
        foreach (var file in files.EnumerateFiles(folder).ToList())
        {
            await files.DeleteFileAsync(file, folder);
        }
    }

    private async Task DeleteAllFilesAsync(string folder)
    {
        foreach (var file in files.EnumerateFiles(folder).ToList())
        {
            await files.DeleteFileAsync(file, folder);
        }
    }
}
