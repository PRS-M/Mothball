using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Infrastructure.Services.JsonStore.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.JsonStore;

public sealed partial class JsonInventoryStore
{
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
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete stale JSON store file {FileName} from {Folder}.", file, slotFolder);
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

    private async Task<bool> IsSlotCompleteAsync(string slot)
    {
        string folder = JsonStoreConstants.SlotFolder(slot);
        foreach (var required in JsonStoreConstants.ExpectedFiles)
        {
            try
            {
                _ = await files.ReadTextFileAsync(required, folder).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "JSON store slot {Slot} is incomplete; missing or unreadable file {FileName}.", slot, required);
                return false;
            }
        }

        return true;
    }

    private async Task<T?> TryReadJsonAsync<T>(string fileName, string folder)
    {
        try
        {
            var raw = await files.ReadTextFileAsync(fileName, folder).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(raw, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to read JSON store file {FileName} from {Folder}.", fileName, folder);
            return default;
        }
    }

    private async Task WriteJsonAsync<T>(string fileName, string folder, T data)
    {
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await files.SaveTextFileAsync(fileName, folder, json).ConfigureAwait(false);
    }
}
