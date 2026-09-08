using System.Text.Json;
using CoreApp.Application.Abstractions.Platform;
using CoreApp.Application.Utilities;
using Infrastructure.Services.JsonStore.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.JsonStore;

/// <summary>
/// Two-slot, multi-file JSON store that supports atomic commits, last-commit rollback,
/// and best-effort recovery. Intended to emulate SQLite repository semantics.
/// </summary>
public sealed partial class JsonInventoryStore
{
    public async Task ReplaceAllPhotosWithSharedAssetsAsync(
        IFileHandler files,
        IProgress<MaintenanceProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        await UpdateAsync(state =>
        {
            var containerIds = state.Containers.Select(container => container.ContainerId).ToHashSet();
            var itemIds = state.Items.Select(item => item.ItemId).ToHashSet();
        for (var index = 0; index < state.Images.Count; index++)
        {
                cancellationToken.ThrowIfCancellationRequested();
                var image = state.Images[index];
                image.StoredFileName = containerIds.Contains(image.OwnerUniqueId) ? "seeded-container.jpg" :
                    itemIds.Contains(image.OwnerUniqueId) ? "seeded-item.jpg" : image.StoredFileName;
                image.IsSharedAsset = containerIds.Contains(image.OwnerUniqueId) || itemIds.Contains(image.OwnerUniqueId);
                image.ImageDataBase64 = null;
                progress?.Report(new MaintenanceProgress((index + 1d) / Math.Max(state.Images.Count, 1), "Replacing photos"));
            }

            return Task.CompletedTask;
        });

        await EnsureSharedAssetAsync(files, "container.png", "seeded-container.jpg");
        await EnsureSharedAssetAsync(files, "mothball_logo.png", "seeded-item.jpg");
        await DeleteFilesAsync(files, Constants.PathToContainerPhotos);
        await DeleteFilesAsync(files, Constants.PathToItemPhotos);
        progress?.Report(new MaintenanceProgress(1, "Photos replaced"));
    }

    public async Task ResetAllDataAsync(
        IFileHandler files,
        IProgress<MaintenanceProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);
        progress?.Report(new MaintenanceProgress(0, "Deleting inventory data", 0));
        await UpdateAsync(state =>
        {
            state.Metadata = new JsonStoreMetadata();
            state.Containers.Clear();
            state.Items.Clear();
            state.Inventories.Clear();
            state.Images.Clear();
            state.Relations.Clear();
            state.Tags.Clear();
            state.TagAssignments.Clear();
            state.Barcodes.Clear();
            return Task.CompletedTask;
        });
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

        await EnsureSharedAssetAsync(files, "container.png", "seeded-container.jpg");
        progress?.Report(new MaintenanceProgress(1, "Data reset complete", 1));
    }

    private async Task EnsureSharedAssetAsync(IFileHandler files, string rawName, string storedName)
    {
        if (!files.FileExists(storedName, Constants.PathToSharedPhotos))
        {
            await files.CopyFileFromRawToAppDataAsync(rawName, storedName, Constants.PathToSharedPhotos);
        }
    }

    private static async Task DeleteFilesAsync(IFileHandler files, string folder)
    {
        foreach (var file in files.EnumerateFiles(folder).ToList())
        {
            await files.DeleteFileAsync(file, folder);
        }
    }

    private readonly IFileHandler files;
    private readonly ILogger<JsonInventoryStore> logger;
    private readonly JsonStoreManifestManager manifestManager;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public JsonInventoryStore(IFileHandler files, ILogger<JsonInventoryStore> logger)
    {
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        manifestManager = new JsonStoreManifestManager(this.files, this.logger, JsonOptions, IsSlotCompleteAsync);
    }

    public async Task<bool> TryRecoverAsync()
    {
        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await TryRecoverUnlockedAsync().ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task<bool> TryRecoverUnlockedAsync()
    {
        // Ensure there is at least one valid manifest+slot.
        // If none exist, initialize empty store into slot A.
        var active = await manifestManager.TryGetActiveAsync();
        if (active is not null) return true;
        try
        {
            active = await manifestManager.TryGetActiveAsync();
            if (active is not null) return true;

            var empty = new StoreState();
            var initial = new JsonStoreManifest
            {
                Generation = 1,
                CurrentSlot = "A",
                PreviousSlot = "A",
                SchemaVersion = empty.Metadata.SchemaVersion,
            };

            await WriteSlotAsync("A", empty, generation: initial.Generation).ConfigureAwait(false);
            await manifestManager.WriteAsync(JsonStoreConstants.ManifestAFileName, initial).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JSON inventory store recovery failed.");
            return false;
        }
    }

    public async Task<bool> TryRollbackLastCommitAsync()
    {
        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var active = await manifestManager.TryGetActiveAsync().ConfigureAwait(false);
            if (active is null) return false;

            // If previous == current, nothing to rollback to.
            if (string.Equals(active.Manifest.PreviousSlot, active.Manifest.CurrentSlot, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var rollback = new JsonStoreManifest
            {
                Generation = active.Manifest.Generation + 1,
                CurrentSlot = active.Manifest.PreviousSlot,
                PreviousSlot = active.Manifest.CurrentSlot,
                SchemaVersion = active.Manifest.SchemaVersion,
            };

            string inactiveManifest = active.InactiveManifestFileName;
            await manifestManager.WriteAsync(inactiveManifest, rollback).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JSON inventory store rollback failed.");
            return false;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<StoreState> LoadAsync()
    {
        var active = await manifestManager.TryGetActiveAsync().ConfigureAwait(false);
        if (active is null)
        {
            // Best-effort auto-recovery for callers who didn't run startup init.
            var recovered = await TryRecoverAsync().ConfigureAwait(false);
            if (!recovered)
            {
                return new StoreState();
            }

            active = await manifestManager.TryGetActiveAsync().ConfigureAwait(false);
            if (active is null) return new StoreState();
        }

        string slotFolder = JsonStoreConstants.SlotFolder(active.Manifest.CurrentSlot);
        return await ReadSlotAsync(slotFolder).ConfigureAwait(false);
    }

    /// <summary>
    /// Applies an update to the active state and commits it as the next JSON store snapshot.
    /// </summary>
    /// <param name="updater">Mutates the in-memory state before it is persisted.</param>
    /// <param name="cancellationToken">Cancels before the next persistence boundary.</param>
    public async Task UpdateAsync(
        Func<StoreState, Task> updater,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updater);

        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var active = await manifestManager.TryGetActiveAsync().ConfigureAwait(false);
            if (active is null)
            {
                var recovered = await TryRecoverUnlockedAsync().ConfigureAwait(false);
                if (!recovered) throw new IOException("Failed to initialize JSON store.");
                active = await manifestManager.TryGetActiveAsync().ConfigureAwait(false);
                if (active is null) throw new IOException("Failed to initialize JSON store.");
            }

            var state = await ReadSlotAsync(JsonStoreConstants.SlotFolder(active.Manifest.CurrentSlot)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await updater(state).ConfigureAwait(false);

            string nextSlot = JsonStoreConstants.OtherSlot(active.Manifest.CurrentSlot);
            int nextGeneration = active.Manifest.Generation + 1;

            cancellationToken.ThrowIfCancellationRequested();
            await WriteSlotAsync(nextSlot, state, nextGeneration).ConfigureAwait(false);

            var nextManifest = new JsonStoreManifest
            {
                Generation = nextGeneration,
                CurrentSlot = nextSlot,
                PreviousSlot = active.Manifest.CurrentSlot,
                SchemaVersion = state.Metadata.SchemaVersion,
            };

            cancellationToken.ThrowIfCancellationRequested();
            await manifestManager.WriteAsync(active.InactiveManifestFileName, nextManifest).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }
}
