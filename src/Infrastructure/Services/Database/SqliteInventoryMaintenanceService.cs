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
    private readonly IInventoryChangeTracker inventoryChanges;

    public SqliteInventoryMaintenanceService(
        MothballDatabase database,
        IFileHandler files,
        IInventoryChangeTracker inventoryChanges)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.inventoryChanges = inventoryChanges ?? throw new ArgumentNullException(nameof(inventoryChanges));
    }

    public async Task ReplaceAllPhotosWithSharedAssetsAsync(IProgress<MaintenanceProgress>? progress = null, CancellationToken cancellationToken = default)
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
            cancellationToken.ThrowIfCancellationRequested();
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

    public async Task ResetAllDataAsync(IProgress<MaintenanceProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new MaintenanceProgress(0, "Deleting inventory data", 0));
        await database.ResetAsync();
        inventoryChanges.MarkChanged();
        progress?.Report(new MaintenanceProgress(0.25, "Deleting photo files", 0));

        var folders = new[]
        {
            Constants.PathToContainerPhotos,
            Constants.PathToItemPhotos,
            Constants.PathToSharedPhotos,
        };
        var filesToDelete = folders
            .SelectMany(folder => files.EnumerateFiles(folder).Select(file => (folder, file)))
            .ToList();

        for (var index = 0; index < filesToDelete.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (folder, file) = filesToDelete[index];
            await files.DeleteFileAsync(file, folder, cancellationToken);
            var stepProgress = (index + 1d) / Math.Max(filesToDelete.Count, 1);
            progress?.Report(new MaintenanceProgress(0.25 + stepProgress * 0.75, "Deleting photo files", stepProgress));
        }

        await EnsureSharedAssetAsync("container.png", SharedContainerPhoto);
        progress?.Report(new MaintenanceProgress(1, "Data reset complete", 1));
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
