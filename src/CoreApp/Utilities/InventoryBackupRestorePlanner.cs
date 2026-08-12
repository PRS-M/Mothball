using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CoreApp.Contracts;

namespace CoreApp.Utilities;

public static class InventoryBackupRestorePlanner
{
    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions CanonicalDataJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static InventoryBackupEnvelope ParseBackupJson(string backupJson)
    {
        if (string.IsNullOrWhiteSpace(backupJson))
        {
            throw new ArgumentException("Backup JSON cannot be null or empty.", nameof(backupJson));
        }

        InventoryBackupEnvelope? backup;
        try
        {
            backup = JsonSerializer.Deserialize<InventoryBackupEnvelope>(backupJson, BackupJsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson), ex);
        }

        if (backup is null)
        {
            throw new ArgumentException("Backup JSON payload is invalid.", nameof(backupJson));
        }

        return backup;
    }

    public static void ValidatePayloadVersion(InventoryBackupEnvelope backup)
    {
        ArgumentNullException.ThrowIfNull(backup);

        if (backup.PayloadVersion != InventoryBackupEnvelope.CurrentPayloadVersion)
        {
            throw new NotSupportedException(
                $"Unsupported backup payload version '{backup.PayloadVersion}'. Expected '{InventoryBackupEnvelope.CurrentPayloadVersion}'.");
        }
    }

    public static InventoryBackupEnvelope AttachIntegrity(
        InventoryBackupEnvelope backup,
        string? signatureSecret = null,
        string? keyId = null)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);

        string checksum = ComputePayloadChecksum(backup.Data);

        string? signature = null;
        string? signatureAlgorithm = null;
        if (!string.IsNullOrWhiteSpace(signatureSecret))
        {
            signature = ComputeHmacSignature(backup.Data, signatureSecret!);
            signatureAlgorithm = InventoryBackupIntegrity.HmacSha256SignatureAlgorithm;
        }

        return backup with
        {
            Integrity = new InventoryBackupIntegrity
            {
                ChecksumAlgorithm = InventoryBackupIntegrity.Sha256Algorithm,
                PayloadChecksum = checksum,
                SignatureAlgorithm = signatureAlgorithm,
                Signature = signature,
                KeyId = keyId,
            },
        };
    }

    public static void ValidateIntegrity(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        ArgumentNullException.ThrowIfNull(options);

        var integrity = backup.Integrity;
        bool hasChecksum = !string.IsNullOrWhiteSpace(integrity?.PayloadChecksum);

        if (!hasChecksum)
        {
            if (options.RequireIntegrityValidation)
            {
                throw new InvalidDataException("Backup integrity metadata is missing.");
            }

            return;
        }

        if (!string.Equals(integrity!.ChecksumAlgorithm, InventoryBackupIntegrity.Sha256Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported checksum algorithm '{integrity.ChecksumAlgorithm}'.");
        }

        string computedChecksum = ComputePayloadChecksum(backup.Data);
        if (!string.Equals(computedChecksum, integrity.PayloadChecksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Backup checksum verification failed.");
        }

        bool hasSignature = !string.IsNullOrWhiteSpace(integrity.Signature);
        bool hasSignatureAlgorithm = !string.IsNullOrWhiteSpace(integrity.SignatureAlgorithm);
        if (hasSignature != hasSignatureAlgorithm)
        {
            throw new InvalidDataException("Backup signature metadata is incomplete.");
        }

        if (!hasSignature)
        {
            return;
        }

        if (!string.Equals(integrity.SignatureAlgorithm, InventoryBackupIntegrity.HmacSha256SignatureAlgorithm, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Unsupported signature algorithm '{integrity.SignatureAlgorithm}'.");
        }

        if (string.IsNullOrWhiteSpace(options.SignatureSecret))
        {
            throw new InvalidDataException("Backup includes a signature but no signature secret was provided.");
        }

        string expected = ComputeHmacSignature(backup.Data, options.SignatureSecret);
        if (!FixedTimeEqualsBase64(expected, integrity.Signature!))
        {
            throw new InvalidDataException("Backup signature verification failed.");
        }
    }

    public static InventoryBackupRestorePlan BuildPlan(
        InventoryBackupEnvelope backup,
        InventoryBackupExistingState existingState,
        InventoryBackupConflictPolicy conflictPolicy)
    {
        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        ArgumentNullException.ThrowIfNull(existingState);

        var context = new PlannerContext(existingState, conflictPolicy);

        PlanContainers(backup, context);
        PlanItems(backup, context);
        ApplyRootSyncFilters(context);

        if (context.IsStrictFullSync)
        {
            PlanRelationsStrict(backup, context);
            PlanImagesStrict(backup, context);
        }
        else
        {
            PlanRelationsAdditive(backup, context);
            PlanImagesAdditive(backup, context);
        }

        return BuildPlanResult(context);
    }

    private static void PlanContainers(InventoryBackupEnvelope backup, PlannerContext context)
    {
        foreach (var container in backup.Data.Containers)
        {
            context.BackupContainerIds.Add(container.ContainerId);

            if (context.ExistingContainersById.TryGetValue(container.ContainerId, out var existing))
            {
                bool shouldUpdate = context.ConflictPolicy != InventoryBackupConflictPolicy.AddOnly
                    && (!string.Equals(existing.Name, container.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Notes, container.Notes, StringComparison.Ordinal));

                if (shouldUpdate)
                {
                    context.ContainersToUpdate.Add(container);
                }
                else
                {
                    context.SkippedExistingContainers++;
                }

                continue;
            }

            context.KnownContainerIds.Add(container.ContainerId);
            context.ContainersToInsert.Add(container);
        }
    }

    private static void PlanItems(InventoryBackupEnvelope backup, PlannerContext context)
    {
        foreach (var item in backup.Data.Items)
        {
            context.BackupItemIds.Add(item.ItemId);

            if (context.ExistingItemsById.TryGetValue(item.ItemId, out var existing))
            {
                bool shouldUpdate = context.ConflictPolicy != InventoryBackupConflictPolicy.AddOnly
                    && (!string.Equals(existing.Name, item.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Description, item.Description, StringComparison.Ordinal));

                if (shouldUpdate)
                {
                    context.ItemsToUpdate.Add(item);
                }
                else
                {
                    context.SkippedExistingItems++;
                }

                continue;
            }

            context.KnownItemIds.Add(item.ItemId);
            context.ItemsToInsert.Add(item);
        }
    }

    private static void ApplyRootSyncFilters(PlannerContext context)
    {
        if (!context.IsFullSyncRoots)
        {
            return;
        }

        foreach (var existingContainerId in context.ExistingContainersById.Keys)
        {
            if (!context.BackupContainerIds.Contains(existingContainerId))
            {
                context.ContainerIdsToDelete.Add(existingContainerId);
            }
        }

        foreach (var existingItemId in context.ExistingItemsById.Keys)
        {
            if (!context.BackupItemIds.Contains(existingItemId))
            {
                context.ItemIdsToDelete.Add(existingItemId);
            }
        }

        context.KnownContainerIds = context.BackupContainerIds;
        context.KnownItemIds = context.BackupItemIds;

        var relationKeysToRemove = context.KnownRelationQuantityByPair.Keys
            .Where(key => !context.KnownContainerIds.Contains(key.ContainerId) || !context.KnownItemIds.Contains(key.ItemId))
            .ToList();

        foreach (var key in relationKeysToRemove)
        {
            context.KnownRelationQuantityByPair.Remove(key);
        }

        context.KnownContainerImages = context.KnownContainerImages
            .Where(image => context.KnownContainerIds.Contains(image.OwnerId))
            .ToHashSet();

        context.KnownItemImages = context.KnownItemImages
            .Where(image => context.KnownItemIds.Contains(image.OwnerId))
            .ToHashSet();
    }

    private static void PlanRelationsAdditive(InventoryBackupEnvelope backup, PlannerContext context)
    {
        foreach (var relation in backup.Data.Relations)
        {
            if (!IsValidKnownRelation(relation, context))
            {
                continue;
            }

            var key = (relation.ContainerId, relation.ItemId);
            context.KnownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
            if (relation.Quantity <= existingQuantity)
            {
                context.SkippedExistingRelations++;
                continue;
            }

            int missingQuantity = relation.Quantity - existingQuantity;
            context.RelationsToInsert.Add(new InventoryBackupPlannedRelationInsert(
                relation.ContainerId,
                relation.ItemId,
                missingQuantity));

            context.KnownRelationQuantityByPair[key] = relation.Quantity;
            context.AddedRelationQuantity += missingQuantity;
        }
    }

    private static void PlanRelationsStrict(InventoryBackupEnvelope backup, PlannerContext context)
    {
        var backupRelationQuantityByPair = new Dictionary<(Guid ContainerId, Guid ItemId), int>();

        foreach (var relation in backup.Data.Relations)
        {
            if (!IsValidKnownRelation(relation, context))
            {
                continue;
            }

            var key = (relation.ContainerId, relation.ItemId);
            backupRelationQuantityByPair.TryGetValue(key, out int current);
            backupRelationQuantityByPair[key] = current + relation.Quantity;
        }

        foreach (var (key, desiredQuantity) in backupRelationQuantityByPair)
        {
            context.KnownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
            if (existingQuantity == desiredQuantity)
            {
                context.SkippedExistingRelations++;
                continue;
            }

            context.RelationsToSet.Add(new InventoryBackupPlannedRelationSet(key.ContainerId, key.ItemId, desiredQuantity));
            if (desiredQuantity > existingQuantity)
            {
                context.AddedRelationQuantity += desiredQuantity - existingQuantity;
            }
        }

        foreach (var key in context.KnownRelationQuantityByPair.Keys)
        {
            if (!backupRelationQuantityByPair.ContainsKey(key))
            {
                context.RelationsToDelete.Add(new InventoryBackupPlannedRelationDelete(key.ContainerId, key.ItemId));
            }
        }
    }

    private static bool IsValidKnownRelation(InventoryBackupRelation relation, PlannerContext context)
    {
        if (relation.Quantity <= 0)
        {
            context.SkippedInvalidRelations++;
            return false;
        }

        if (!context.KnownContainerIds.Contains(relation.ContainerId) || !context.KnownItemIds.Contains(relation.ItemId))
        {
            context.SkippedInvalidRelations++;
            return false;
        }

        return true;
    }

    private static void PlanImagesAdditive(InventoryBackupEnvelope backup, PlannerContext context)
    {
        foreach (var image in backup.Data.Images)
        {
            if (image.OwnerType == InventoryBackupOwnerType.Container)
            {
                if (!context.KnownContainerIds.Contains(image.OwnerId))
                {
                    context.SkippedImagesWithMissingOwner++;
                    continue;
                }

                var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                if (!context.KnownContainerImages.Add(ownership))
                {
                    context.SkippedExistingImages++;
                    continue;
                }

                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    image.OwnerId,
                    image.ImageId,
                    InventoryBackupOwnerType.Container));
                continue;
            }

            if (image.OwnerType == InventoryBackupOwnerType.Item)
            {
                if (!context.KnownItemIds.Contains(image.OwnerId))
                {
                    context.SkippedImagesWithMissingOwner++;
                    continue;
                }

                var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                if (!context.KnownItemImages.Add(ownership))
                {
                    context.SkippedExistingImages++;
                    continue;
                }

                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    image.OwnerId,
                    image.ImageId,
                    InventoryBackupOwnerType.Item));
                continue;
            }

            context.SkippedImagesWithMissingOwner++;
        }
    }

    private static void PlanImagesStrict(InventoryBackupEnvelope backup, PlannerContext context)
    {
        var backupContainerImages = new HashSet<InventoryBackupImageOwnership>();
        var backupItemImages = new HashSet<InventoryBackupImageOwnership>();

        foreach (var image in backup.Data.Images)
        {
            if (image.OwnerType == InventoryBackupOwnerType.Container)
            {
                if (!context.KnownContainerIds.Contains(image.OwnerId))
                {
                    context.SkippedImagesWithMissingOwner++;
                    continue;
                }

                backupContainerImages.Add(new InventoryBackupImageOwnership(image.OwnerId, image.ImageId));
                continue;
            }

            if (image.OwnerType == InventoryBackupOwnerType.Item)
            {
                if (!context.KnownItemIds.Contains(image.OwnerId))
                {
                    context.SkippedImagesWithMissingOwner++;
                    continue;
                }

                backupItemImages.Add(new InventoryBackupImageOwnership(image.OwnerId, image.ImageId));
                continue;
            }

            context.SkippedImagesWithMissingOwner++;
        }

        foreach (var ownership in backupContainerImages)
        {
            if (!context.KnownContainerImages.Contains(ownership))
            {
                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    ownership.OwnerId,
                    ownership.ImageId,
                    InventoryBackupOwnerType.Container));
            }
            else
            {
                context.SkippedExistingImages++;
            }
        }

        foreach (var ownership in backupItemImages)
        {
            if (!context.KnownItemImages.Contains(ownership))
            {
                context.ImagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                    ownership.OwnerId,
                    ownership.ImageId,
                    InventoryBackupOwnerType.Item));
            }
            else
            {
                context.SkippedExistingImages++;
            }
        }

        foreach (var existingImage in context.KnownContainerImages)
        {
            if (!backupContainerImages.Contains(existingImage))
            {
                context.ImagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                    existingImage.OwnerId,
                    existingImage.ImageId,
                    InventoryBackupOwnerType.Container));
            }
        }

        foreach (var existingImage in context.KnownItemImages)
        {
            if (!backupItemImages.Contains(existingImage))
            {
                context.ImagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                    existingImage.OwnerId,
                    existingImage.ImageId,
                    InventoryBackupOwnerType.Item));
            }
        }
    }

    private static InventoryBackupRestorePlan BuildPlanResult(PlannerContext context)
    {
        var result = new InventoryBackupRestoreResult
        {
            AddedContainers = context.ContainersToInsert.Count,
            AddedItems = context.ItemsToInsert.Count,
            AddedRelations = context.RelationsToInsert.Count + context.RelationsToSet.Count,
            AddedRelationQuantity = context.AddedRelationQuantity,
            AddedImages = context.ImagesToInsert.Count,
            UpdatedContainers = context.ContainersToUpdate.Count,
            UpdatedItems = context.ItemsToUpdate.Count,
            DeletedContainers = context.ContainerIdsToDelete.Count,
            DeletedItems = context.ItemIdsToDelete.Count,
            DeletedRelations = context.RelationsToDelete.Count,
            DeletedImages = context.ImagesToDelete.Count,
            SkippedExistingContainers = context.SkippedExistingContainers,
            SkippedExistingItems = context.SkippedExistingItems,
            SkippedExistingRelations = context.SkippedExistingRelations,
            SkippedExistingImages = context.SkippedExistingImages,
            SkippedInvalidRelations = context.SkippedInvalidRelations,
            SkippedImagesWithMissingOwner = context.SkippedImagesWithMissingOwner,
        };

        return new InventoryBackupRestorePlan(
            context.ContainersToInsert,
            context.ContainersToUpdate,
            context.ContainerIdsToDelete,
            context.ItemsToInsert,
            context.ItemsToUpdate,
            context.ItemIdsToDelete,
            context.RelationsToInsert,
            context.RelationsToSet,
            context.RelationsToDelete,
            context.ImagesToInsert,
            context.ImagesToDelete,
            result);
    }

    private sealed class PlannerContext
    {
        public PlannerContext(InventoryBackupExistingState existingState, InventoryBackupConflictPolicy conflictPolicy)
        {
            ConflictPolicy = conflictPolicy;
            ExistingContainersById = existingState.Containers.ToDictionary(c => c.ContainerId, c => c);
            ExistingItemsById = existingState.Items.ToDictionary(i => i.ItemId, i => i);

            KnownContainerIds = ExistingContainersById.Keys.ToHashSet();
            KnownItemIds = ExistingItemsById.Keys.ToHashSet();
            KnownContainerImages = existingState.ContainerImages.ToHashSet();
            KnownItemImages = existingState.ItemImages.ToHashSet();
            KnownRelationQuantityByPair = existingState.Relations
                .GroupBy(r => (r.ContainerId, r.ItemId))
                .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));
        }

        public InventoryBackupConflictPolicy ConflictPolicy { get; }
        public bool IsFullSyncRoots => ConflictPolicy is InventoryBackupConflictPolicy.FullSync or InventoryBackupConflictPolicy.StrictFullSync;
        public bool IsStrictFullSync => ConflictPolicy == InventoryBackupConflictPolicy.StrictFullSync;

        public Dictionary<Guid, InventoryBackupExistingContainer> ExistingContainersById { get; }
        public Dictionary<Guid, InventoryBackupExistingItem> ExistingItemsById { get; }

        public HashSet<Guid> KnownContainerIds { get; set; }
        public HashSet<Guid> KnownItemIds { get; set; }
        public HashSet<InventoryBackupImageOwnership> KnownContainerImages { get; set; }
        public HashSet<InventoryBackupImageOwnership> KnownItemImages { get; set; }
        public Dictionary<(Guid ContainerId, Guid ItemId), int> KnownRelationQuantityByPair { get; }

        public HashSet<Guid> BackupContainerIds { get; } = [];
        public HashSet<Guid> BackupItemIds { get; } = [];

        public List<InventoryBackupContainer> ContainersToInsert { get; } = [];
        public List<InventoryBackupContainer> ContainersToUpdate { get; } = [];
        public List<Guid> ContainerIdsToDelete { get; } = [];
        public List<InventoryBackupItem> ItemsToInsert { get; } = [];
        public List<InventoryBackupItem> ItemsToUpdate { get; } = [];
        public List<Guid> ItemIdsToDelete { get; } = [];
        public List<InventoryBackupPlannedRelationInsert> RelationsToInsert { get; } = [];
        public List<InventoryBackupPlannedRelationSet> RelationsToSet { get; } = [];
        public List<InventoryBackupPlannedRelationDelete> RelationsToDelete { get; } = [];
        public List<InventoryBackupPlannedImageInsert> ImagesToInsert { get; } = [];
        public List<InventoryBackupPlannedImageDelete> ImagesToDelete { get; } = [];

        public int SkippedExistingContainers { get; set; }
        public int SkippedExistingItems { get; set; }
        public int SkippedExistingRelations { get; set; }
        public int SkippedExistingImages { get; set; }
        public int SkippedInvalidRelations { get; set; }
        public int SkippedImagesWithMissingOwner { get; set; }
        public int AddedRelationQuantity { get; set; }
    }

    private static string ComputePayloadChecksum(InventoryBackupData data)
    {
        string canonicalData = JsonSerializer.Serialize(data, CanonicalDataJsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(canonicalData);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string ComputeHmacSignature(InventoryBackupData data, string signatureSecret)
    {
        string canonicalData = JsonSerializer.Serialize(data, CanonicalDataJsonOptions);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(canonicalData);
        byte[] secretBytes = Encoding.UTF8.GetBytes(signatureSecret);

        using var hmac = new HMACSHA256(secretBytes);
        byte[] signatureBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToBase64String(signatureBytes);
    }

    private static bool FixedTimeEqualsBase64(string expectedBase64, string providedBase64)
    {
        byte[] expected;
        byte[] provided;
        try
        {
            expected = Convert.FromBase64String(expectedBase64);
            provided = Convert.FromBase64String(providedBase64);
        }
        catch (FormatException)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }
}
