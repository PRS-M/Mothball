using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Infrastructure.Services.JsonStore.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.JsonStore;

/// <summary>
/// Two-slot, multi-file JSON store that supports atomic commits, last-commit rollback,
/// and best-effort recovery. Intended to emulate SQLite repository semantics.
/// </summary>
public sealed partial class JsonInventoryStore
{
    private readonly IFileHandler files;
    private readonly ILogger<JsonInventoryStore> logger;
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
    }

    public async Task<bool> TryRecoverAsync()
    {
        // Ensure there is at least one valid manifest+slot.
        // If none exist, initialize empty store into slot A.
        var active = await TryGetActiveManifestAsync();
        if (active is not null) return true;

        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            active = await TryGetActiveManifestAsync();
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
            await WriteManifestAsync(JsonStoreConstants.ManifestAFileName, initial).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "JSON inventory store recovery failed.");
            return false;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<bool> TryRollbackLastCommitAsync()
    {
        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var active = await TryGetActiveManifestAsync().ConfigureAwait(false);
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
            await WriteManifestAsync(inactiveManifest, rollback).ConfigureAwait(false);
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
        var active = await TryGetActiveManifestAsync().ConfigureAwait(false);
        if (active is null)
        {
            // Best-effort auto-recovery for callers who didn't run startup init.
            var recovered = await TryRecoverAsync().ConfigureAwait(false);
            if (!recovered)
            {
                return new StoreState();
            }

            active = await TryGetActiveManifestAsync().ConfigureAwait(false);
            if (active is null) return new StoreState();
        }

        string slotFolder = JsonStoreConstants.SlotFolder(active.Manifest.CurrentSlot);
        return await ReadSlotAsync(slotFolder).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Func<StoreState, Task> updater)
    {
        ArgumentNullException.ThrowIfNull(updater);

        await writeLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var active = await TryGetActiveManifestAsync().ConfigureAwait(false);
            if (active is null)
            {
                var recovered = await TryRecoverAsync().ConfigureAwait(false);
                if (!recovered) throw new IOException("Failed to initialize JSON store.");
                active = await TryGetActiveManifestAsync().ConfigureAwait(false);
                if (active is null) throw new IOException("Failed to initialize JSON store.");
            }

            var state = await ReadSlotAsync(JsonStoreConstants.SlotFolder(active.Manifest.CurrentSlot)).ConfigureAwait(false);
            await updater(state).ConfigureAwait(false);

            string nextSlot = JsonStoreConstants.OtherSlot(active.Manifest.CurrentSlot);
            int nextGeneration = active.Manifest.Generation + 1;

            await WriteSlotAsync(nextSlot, state, nextGeneration).ConfigureAwait(false);

            var nextManifest = new JsonStoreManifest
            {
                Generation = nextGeneration,
                CurrentSlot = nextSlot,
                PreviousSlot = active.Manifest.CurrentSlot,
                SchemaVersion = state.Metadata.SchemaVersion,
            };

            await WriteManifestAsync(active.InactiveManifestFileName, nextManifest).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }
}
