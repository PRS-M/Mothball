using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreApp.Interfaces;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

/// <summary>
/// Two-slot, multi-file JSON store that supports atomic commits, last-commit rollback,
/// and best-effort recovery. Intended to emulate SQLite repository semantics.
/// </summary>
public sealed class JsonInventoryStore
{
    private readonly IFileHandler files;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public JsonInventoryStore(IFileHandler files)
    {
        this.files = files;
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
        catch
        {
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
        catch
        {
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

    public sealed class StoreState
    {
        public JsonStoreMetadata Metadata { get; set; } = new();
        public List<JsonContainerRow> Containers { get; set; } = [];
        public List<JsonItemRow> Items { get; set; } = [];
        public List<JsonImageRow> Images { get; set; } = [];
        public List<JsonRelationRow> Relations { get; set; } = [];
    }

    private async Task<StoreState> ReadSlotAsync(string slotFolder)
    {
        // Missing files mean empty store (first-run or partial cleanup).
        var metadata = await TryReadJsonAsync<JsonStoreMetadata>(JsonStoreConstants.MetadataFileName, slotFolder)
                       ?? new JsonStoreMetadata();

        var containers = await TryReadJsonAsync<List<JsonContainerRow>>(JsonStoreConstants.ContainersFileName, slotFolder)
                        ?? [];
        var items = await TryReadJsonAsync<List<JsonItemRow>>(JsonStoreConstants.ItemsFileName, slotFolder)
                   ?? [];
        var images = await TryReadJsonAsync<List<JsonImageRow>>(JsonStoreConstants.ImagesFileName, slotFolder)
                    ?? [];
        var relations = await TryReadJsonAsync<List<JsonRelationRow>>(JsonStoreConstants.RelationsFileName, slotFolder)
                       ?? [];

        // Ensure counters are sane even if metadata is missing/outdated.
        metadata.NextContainerRowId = Math.Max(metadata.NextContainerRowId, containers.Select(c => c.RowId).DefaultIfEmpty(0).Max() + 1);
        metadata.NextItemRowId = Math.Max(metadata.NextItemRowId, items.Select(i => i.RowId).DefaultIfEmpty(0).Max() + 1);
        metadata.NextImageRowId = Math.Max(metadata.NextImageRowId, images.Select(p => p.RowId).DefaultIfEmpty(0).Max() + 1);
        metadata.NextRelationId = Math.Max(metadata.NextRelationId, relations.Select(r => r.Id).DefaultIfEmpty(0).Max() + 1);

        return new StoreState
        {
            Metadata = metadata,
            Containers = containers,
            Items = items,
            Images = images,
            Relations = relations,
        };
    }

    private async Task WriteSlotAsync(string slot, StoreState state, int generation)
    {
        string slotFolder = JsonStoreConstants.SlotFolder(slot);

        // Clear the slot (best-effort).
        foreach (var file in files.EnumerateFiles(slotFolder, "*.json").ToList())
        {
            try
            {
                await files.DeleteFileAsync(file, slotFolder).ConfigureAwait(false);
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }

        // Write metadata + tables.
        await WriteJsonAsync(JsonStoreConstants.MetadataFileName, slotFolder, state.Metadata).ConfigureAwait(false);
        await WriteJsonAsync(JsonStoreConstants.ContainersFileName, slotFolder, state.Containers).ConfigureAwait(false);
        await WriteJsonAsync(JsonStoreConstants.ItemsFileName, slotFolder, state.Items).ConfigureAwait(false);
        await WriteJsonAsync(JsonStoreConstants.ImagesFileName, slotFolder, state.Images).ConfigureAwait(false);
        await WriteJsonAsync(JsonStoreConstants.RelationsFileName, slotFolder, state.Relations).ConfigureAwait(false);

        // Commit info written last inside the slot.
        var commitInfo = new JsonStoreCommitInfo
        {
            Generation = generation,
            CommitId = Guid.NewGuid(),
            CommittedUtc = DateTimeOffset.UtcNow,
        };
        await WriteJsonAsync(JsonStoreConstants.CommitInfoFileName, slotFolder, commitInfo).ConfigureAwait(false);
    }

    private sealed record ActiveManifest(JsonStoreManifest Manifest, string ActiveManifestFileName, string InactiveManifestFileName);

    private async Task<ActiveManifest?> TryGetActiveManifestAsync()
    {
        var ma = await TryReadJsonAsync<JsonStoreManifest>(JsonStoreConstants.ManifestAFileName, JsonStoreConstants.StoreRoot).ConfigureAwait(false);
        var mb = await TryReadJsonAsync<JsonStoreManifest>(JsonStoreConstants.ManifestBFileName, JsonStoreConstants.StoreRoot).ConfigureAwait(false);

        (JsonStoreManifest manifest, string fileName)? best = null;

        if (ma is not null && await IsManifestValidAsync(ma).ConfigureAwait(false)) best = (ma, JsonStoreConstants.ManifestAFileName);
        if (mb is not null && await IsManifestValidAsync(mb).ConfigureAwait(false))
        {
            if (best is null || mb.Generation > best.Value.manifest.Generation) best = (mb, JsonStoreConstants.ManifestBFileName);
        }

        // If the latest manifest points to an invalid current slot but a valid previous slot,
        // accept it by synthesizing a rollback manifest.
        if (best is not null)
        {
            bool currentOk = await IsSlotCompleteAsync(best.Value.manifest.CurrentSlot).ConfigureAwait(false);
            if (!currentOk)
            {
                bool prevOk = await IsSlotCompleteAsync(best.Value.manifest.PreviousSlot).ConfigureAwait(false);
                if (prevOk)
                {
                    var synthesized = new JsonStoreManifest
                    {
                        Generation = best.Value.manifest.Generation,
                        CurrentSlot = best.Value.manifest.PreviousSlot,
                        PreviousSlot = best.Value.manifest.CurrentSlot,
                        SchemaVersion = best.Value.manifest.SchemaVersion,
                    };

                    return new ActiveManifest(
                        synthesized,
                        best.Value.fileName,
                        best.Value.fileName == JsonStoreConstants.ManifestAFileName ? JsonStoreConstants.ManifestBFileName : JsonStoreConstants.ManifestAFileName);
                }
            }
        }

        if (best is null) return null;

        string inactive = best.Value.fileName == JsonStoreConstants.ManifestAFileName
            ? JsonStoreConstants.ManifestBFileName
            : JsonStoreConstants.ManifestAFileName;
        return new ActiveManifest(best.Value.manifest, best.Value.fileName, inactive);
    }

    private async Task<bool> IsManifestValidAsync(JsonStoreManifest manifest)
    {
        if (manifest.Generation <= 0) return false;
        if (!IsSlotValue(manifest.CurrentSlot) || !IsSlotValue(manifest.PreviousSlot)) return false;
        return await IsSlotCompleteAsync(manifest.CurrentSlot).ConfigureAwait(false);
    }

    private async Task<bool> IsSlotCompleteAsync(string slot)
    {
        string folder = JsonStoreConstants.SlotFolder(slot);
        foreach (var required in JsonStoreConstants.ExpectedFiles)
        {
            try
            {
                _ = await files.ReadTextFileAsync(required, folder).ConfigureAwait(false);
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsSlotValue(string value) =>
        value.Equals("A", StringComparison.OrdinalIgnoreCase) || value.Equals("B", StringComparison.OrdinalIgnoreCase);

    private async Task WriteManifestAsync(string manifestFileName, JsonStoreManifest manifest)
    {
        await WriteJsonAsync(manifestFileName, JsonStoreConstants.StoreRoot, manifest).ConfigureAwait(false);
    }

    private async Task<T?> TryReadJsonAsync<T>(string fileName, string folder)
    {
        try
        {
            var raw = await files.ReadTextFileAsync(fileName, folder).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(raw, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private async Task WriteJsonAsync<T>(string fileName, string folder, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await files.SaveTextFileAsync(fileName, folder, json).ConfigureAwait(false);
    }
}
