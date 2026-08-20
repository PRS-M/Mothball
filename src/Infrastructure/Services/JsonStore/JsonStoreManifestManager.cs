using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Infrastructure.Services.JsonStore.Models;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.JsonStore;

internal sealed record JsonStoreActiveManifest(
    JsonStoreManifest Manifest,
    string ActiveManifestFileName,
    string InactiveManifestFileName);

internal sealed class JsonStoreManifestManager
{
    private readonly IFileHandler files;
    private readonly ILogger<JsonInventoryStore> logger;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly Func<string, Task<bool>> isSlotComplete;

    public JsonStoreManifestManager(
        IFileHandler files,
        ILogger<JsonInventoryStore> logger,
        JsonSerializerOptions jsonOptions,
        Func<string, Task<bool>> isSlotComplete)
    {
        this.files = files;
        this.logger = logger;
        this.jsonOptions = jsonOptions;
        this.isSlotComplete = isSlotComplete;
    }

    public async Task<JsonStoreActiveManifest?> TryGetActiveAsync()
    {
        var candidates = new List<ManifestCandidate>(capacity: 2);

        var manifestA = await TryReadCandidateAsync(JsonStoreConstants.ManifestAFileName).ConfigureAwait(false);
        if (manifestA is not null) candidates.Add(manifestA);

        var manifestB = await TryReadCandidateAsync(JsonStoreConstants.ManifestBFileName).ConfigureAwait(false);
        if (manifestB is not null) candidates.Add(manifestB);

        var best = candidates
            .OrderByDescending(candidate => candidate.Manifest.Generation)
            .FirstOrDefault();

        if (best is null || (!best.CurrentSlotComplete && !best.PreviousSlotComplete)) return null;

        var effectiveManifest = best.CurrentSlotComplete
            ? best.Manifest
            : SynthesizeRollback(best.Manifest);

        return new JsonStoreActiveManifest(
            effectiveManifest,
            best.FileName,
            OtherManifest(best.FileName));
    }

    public Task WriteAsync(string manifestFileName, JsonStoreManifest manifest)
    {
        var json = JsonSerializer.Serialize(manifest, jsonOptions);
        return files.SaveTextFileAsync(manifestFileName, JsonStoreConstants.StoreRoot, json);
    }

    private async Task<ManifestCandidate?> TryReadCandidateAsync(string manifestFileName)
    {
        var manifest = await TryReadAsync(manifestFileName).ConfigureAwait(false);
        if (manifest is null || !IsStructureValid(manifest)) return null;

        var currentSlotComplete = await isSlotComplete(manifest.CurrentSlot).ConfigureAwait(false);
        var previousSlotComplete = await isSlotComplete(manifest.PreviousSlot).ConfigureAwait(false);

        return new ManifestCandidate(manifest, manifestFileName, currentSlotComplete, previousSlotComplete);
    }

    private async Task<JsonStoreManifest?> TryReadAsync(string manifestFileName)
    {
        try
        {
            var raw = await files.ReadTextFileAsync(manifestFileName, JsonStoreConstants.StoreRoot).ConfigureAwait(false);
            return JsonSerializer.Deserialize<JsonStoreManifest>(raw, jsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to read JSON store file {FileName} from {Folder}.", manifestFileName, JsonStoreConstants.StoreRoot);
            return null;
        }
    }

    private static bool IsStructureValid(JsonStoreManifest manifest)
        => manifest.Generation > 0
            && IsSlotValue(manifest.CurrentSlot)
            && IsSlotValue(manifest.PreviousSlot);

    private static JsonStoreManifest SynthesizeRollback(JsonStoreManifest manifest)
        => new()
        {
            Generation = manifest.Generation,
            CurrentSlot = manifest.PreviousSlot,
            PreviousSlot = manifest.CurrentSlot,
            SchemaVersion = manifest.SchemaVersion,
        };

    private static string OtherManifest(string fileName)
        => fileName == JsonStoreConstants.ManifestAFileName
            ? JsonStoreConstants.ManifestBFileName
            : JsonStoreConstants.ManifestAFileName;

    private static bool IsSlotValue(string value)
        => value.Equals("A", StringComparison.OrdinalIgnoreCase)
            || value.Equals("B", StringComparison.OrdinalIgnoreCase);

    private sealed record ManifestCandidate(
        JsonStoreManifest Manifest,
        string FileName,
        bool CurrentSlotComplete,
        bool PreviousSlotComplete);
}