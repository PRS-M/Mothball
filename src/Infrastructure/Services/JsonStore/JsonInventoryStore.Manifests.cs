using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Infrastructure.Services.JsonStore.Models;

namespace Infrastructure.Services.JsonStore;

public sealed partial class JsonInventoryStore
{
    private sealed record ActiveManifest(JsonStoreManifest Manifest, string ActiveManifestFileName, string InactiveManifestFileName);

    private sealed record ManifestCandidate(
        JsonStoreManifest Manifest,
        string FileName,
        bool CurrentSlotComplete,
        bool PreviousSlotComplete);

    private async Task<ActiveManifest?> TryGetActiveManifestAsync()
    {
        var candidates = new List<ManifestCandidate>(capacity: 2);

        var a = await TryReadCandidateAsync(JsonStoreConstants.ManifestAFileName).ConfigureAwait(false);
        if (a is not null) candidates.Add(a);

        var b = await TryReadCandidateAsync(JsonStoreConstants.ManifestBFileName).ConfigureAwait(false);
        if (b is not null) candidates.Add(b);

        var best = candidates
            .OrderByDescending(c => c.Manifest.Generation)
            .FirstOrDefault();

        if (best is null) return null;
        if (!best.CurrentSlotComplete && !best.PreviousSlotComplete) return null;

        var effective = best.CurrentSlotComplete
            ? best.Manifest
            : SynthesizeRollback(best.Manifest);

        return new ActiveManifest(
            effective,
            best.FileName,
            OtherManifest(best.FileName));
    }

    private async Task<ManifestCandidate?> TryReadCandidateAsync(string manifestFileName)
    {
        var manifest = await TryReadJsonAsync<JsonStoreManifest>(manifestFileName, JsonStoreConstants.StoreRoot).ConfigureAwait(false);
        if (manifest is null) return null;
        if (!IsManifestStructureValid(manifest)) return null;

        bool currentOk = await IsSlotCompleteAsync(manifest.CurrentSlot).ConfigureAwait(false);
        bool prevOk = await IsSlotCompleteAsync(manifest.PreviousSlot).ConfigureAwait(false);

        return new ManifestCandidate(manifest, manifestFileName, currentOk, prevOk);
    }

    private static bool IsManifestStructureValid(JsonStoreManifest manifest)
    {
        if (manifest.Generation <= 0) return false;
        if (!IsSlotValue(manifest.CurrentSlot) || !IsSlotValue(manifest.PreviousSlot)) return false;
        return true;
    }

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

    private static bool IsSlotValue(string value) =>
        value.Equals("A", StringComparison.OrdinalIgnoreCase) || value.Equals("B", StringComparison.OrdinalIgnoreCase);

    private async Task WriteManifestAsync(string manifestFileName, JsonStoreManifest manifest)
    {
        await WriteJsonAsync(manifestFileName, JsonStoreConstants.StoreRoot, manifest).ConfigureAwait(false);
    }
}
