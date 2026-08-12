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

        var existingContainersById = existingState.Containers.ToDictionary(c => c.ContainerId, c => c);
        var existingItemsById = existingState.Items.ToDictionary(i => i.ItemId, i => i);

        var knownContainerIds = existingContainersById.Keys.ToHashSet();
        var knownItemIds = existingItemsById.Keys.ToHashSet();
        var knownContainerImages = existingState.ContainerImages.ToHashSet();
        var knownItemImages = existingState.ItemImages.ToHashSet();
        var knownRelationQuantityByPair = existingState.Relations
            .GroupBy(r => (r.ContainerId, r.ItemId))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Quantity));

        bool isFullSyncRoots = conflictPolicy is InventoryBackupConflictPolicy.FullSync or InventoryBackupConflictPolicy.StrictFullSync;
        bool isStrictFullSync = conflictPolicy == InventoryBackupConflictPolicy.StrictFullSync;

        var containersToInsert = new List<InventoryBackupContainer>();
        var containersToUpdate = new List<InventoryBackupContainer>();
        var containerIdsToDelete = new List<Guid>();
        var itemsToInsert = new List<InventoryBackupItem>();
        var itemsToUpdate = new List<InventoryBackupItem>();
        var itemIdsToDelete = new List<Guid>();
        var relationsToInsert = new List<InventoryBackupPlannedRelationInsert>();
        var relationsToSet = new List<InventoryBackupPlannedRelationSet>();
        var relationsToDelete = new List<InventoryBackupPlannedRelationDelete>();
        var imagesToInsert = new List<InventoryBackupPlannedImageInsert>();
        var imagesToDelete = new List<InventoryBackupPlannedImageDelete>();

        int skippedExistingContainers = 0;
        int skippedExistingItems = 0;
        int skippedExistingRelations = 0;
        int skippedExistingImages = 0;
        int skippedInvalidRelations = 0;
        int skippedImagesWithMissingOwner = 0;

        int addedRelationQuantity = 0;

        var backupContainerIds = new HashSet<Guid>();
        foreach (var container in backup.Data.Containers)
        {
            backupContainerIds.Add(container.ContainerId);

            if (existingContainersById.TryGetValue(container.ContainerId, out var existing))
            {
                if (conflictPolicy != InventoryBackupConflictPolicy.AddOnly
                    && (!string.Equals(existing.Name, container.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Notes, container.Notes, StringComparison.Ordinal)))
                {
                    containersToUpdate.Add(container);
                    continue;
                }

                skippedExistingContainers++;
                continue;
            }

            knownContainerIds.Add(container.ContainerId);
            containersToInsert.Add(container);
        }

        var backupItemIds = new HashSet<Guid>();
        foreach (var item in backup.Data.Items)
        {
            backupItemIds.Add(item.ItemId);

            if (existingItemsById.TryGetValue(item.ItemId, out var existing))
            {
                if (conflictPolicy != InventoryBackupConflictPolicy.AddOnly
                    && (!string.Equals(existing.Name, item.Name, StringComparison.Ordinal)
                    || !string.Equals(existing.Description, item.Description, StringComparison.Ordinal)))
                {
                    itemsToUpdate.Add(item);
                    continue;
                }

                skippedExistingItems++;
                continue;
            }

            knownItemIds.Add(item.ItemId);
            itemsToInsert.Add(item);
        }

        if (isFullSyncRoots)
        {
            foreach (var existingContainerId in existingContainersById.Keys)
            {
                if (!backupContainerIds.Contains(existingContainerId))
                {
                    containerIdsToDelete.Add(existingContainerId);
                }
            }

            foreach (var existingItemId in existingItemsById.Keys)
            {
                if (!backupItemIds.Contains(existingItemId))
                {
                    itemIdsToDelete.Add(existingItemId);
                }
            }

            knownContainerIds = backupContainerIds;
            knownItemIds = backupItemIds;

            var relationKeysToRemove = knownRelationQuantityByPair.Keys
                .Where(key => !knownContainerIds.Contains(key.ContainerId) || !knownItemIds.Contains(key.ItemId))
                .ToList();
            foreach (var key in relationKeysToRemove)
            {
                knownRelationQuantityByPair.Remove(key);
            }

            knownContainerImages = knownContainerImages
                .Where(image => knownContainerIds.Contains(image.OwnerId))
                .ToHashSet();
            knownItemImages = knownItemImages
                .Where(image => knownItemIds.Contains(image.OwnerId))
                .ToHashSet();
        }

        if (isStrictFullSync)
        {
            var backupRelationQuantityByPair = new Dictionary<(Guid ContainerId, Guid ItemId), int>();
            foreach (var relation in backup.Data.Relations)
            {
                if (relation.Quantity <= 0)
                {
                    skippedInvalidRelations++;
                    continue;
                }

                if (!knownContainerIds.Contains(relation.ContainerId) || !knownItemIds.Contains(relation.ItemId))
                {
                    skippedInvalidRelations++;
                    continue;
                }

                var key = (relation.ContainerId, relation.ItemId);
                backupRelationQuantityByPair.TryGetValue(key, out int current);
                backupRelationQuantityByPair[key] = current + relation.Quantity;
            }

            foreach (var (key, desiredQuantity) in backupRelationQuantityByPair)
            {
                knownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
                if (existingQuantity == desiredQuantity)
                {
                    skippedExistingRelations++;
                    continue;
                }

                relationsToSet.Add(new InventoryBackupPlannedRelationSet(key.ContainerId, key.ItemId, desiredQuantity));
                if (desiredQuantity > existingQuantity)
                {
                    addedRelationQuantity += desiredQuantity - existingQuantity;
                }
            }

            foreach (var key in knownRelationQuantityByPair.Keys)
            {
                if (!backupRelationQuantityByPair.ContainsKey(key))
                {
                    relationsToDelete.Add(new InventoryBackupPlannedRelationDelete(key.ContainerId, key.ItemId));
                }
            }

            var backupContainerImages = new HashSet<InventoryBackupImageOwnership>();
            var backupItemImages = new HashSet<InventoryBackupImageOwnership>();

            foreach (var image in backup.Data.Images)
            {
                if (image.OwnerType == InventoryBackupOwnerType.Container)
                {
                    if (!knownContainerIds.Contains(image.OwnerId))
                    {
                        skippedImagesWithMissingOwner++;
                        continue;
                    }

                    backupContainerImages.Add(new InventoryBackupImageOwnership(image.OwnerId, image.ImageId));
                    continue;
                }

                if (image.OwnerType == InventoryBackupOwnerType.Item)
                {
                    if (!knownItemIds.Contains(image.OwnerId))
                    {
                        skippedImagesWithMissingOwner++;
                        continue;
                    }

                    backupItemImages.Add(new InventoryBackupImageOwnership(image.OwnerId, image.ImageId));
                    continue;
                }

                skippedImagesWithMissingOwner++;
            }

            foreach (var ownership in backupContainerImages)
            {
                if (!knownContainerImages.Contains(ownership))
                {
                    imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                        ownership.OwnerId,
                        ownership.ImageId,
                        InventoryBackupOwnerType.Container));
                }
                else
                {
                    skippedExistingImages++;
                }
            }

            foreach (var ownership in backupItemImages)
            {
                if (!knownItemImages.Contains(ownership))
                {
                    imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                        ownership.OwnerId,
                        ownership.ImageId,
                        InventoryBackupOwnerType.Item));
                }
                else
                {
                    skippedExistingImages++;
                }
            }

            foreach (var existingImage in knownContainerImages)
            {
                if (!backupContainerImages.Contains(existingImage))
                {
                    imagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                        existingImage.OwnerId,
                        existingImage.ImageId,
                        InventoryBackupOwnerType.Container));
                }
            }

            foreach (var existingImage in knownItemImages)
            {
                if (!backupItemImages.Contains(existingImage))
                {
                    imagesToDelete.Add(new InventoryBackupPlannedImageDelete(
                        existingImage.OwnerId,
                        existingImage.ImageId,
                        InventoryBackupOwnerType.Item));
                }
            }
        }
        else
        {
            foreach (var relation in backup.Data.Relations)
            {
                if (relation.Quantity <= 0)
                {
                    skippedInvalidRelations++;
                    continue;
                }

                if (!knownContainerIds.Contains(relation.ContainerId) || !knownItemIds.Contains(relation.ItemId))
                {
                    skippedInvalidRelations++;
                    continue;
                }

                var key = (relation.ContainerId, relation.ItemId);
                knownRelationQuantityByPair.TryGetValue(key, out int existingQuantity);
                if (relation.Quantity <= existingQuantity)
                {
                    skippedExistingRelations++;
                    continue;
                }

                int missingQuantity = relation.Quantity - existingQuantity;
                relationsToInsert.Add(new InventoryBackupPlannedRelationInsert(
                    relation.ContainerId,
                    relation.ItemId,
                    missingQuantity));

                knownRelationQuantityByPair[key] = relation.Quantity;
                addedRelationQuantity += missingQuantity;
            }

            foreach (var image in backup.Data.Images)
            {
                if (image.OwnerType == InventoryBackupOwnerType.Container)
                {
                    if (!knownContainerIds.Contains(image.OwnerId))
                    {
                        skippedImagesWithMissingOwner++;
                        continue;
                    }

                    var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                    if (!knownContainerImages.Add(ownership))
                    {
                        skippedExistingImages++;
                        continue;
                    }

                    imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                        image.OwnerId,
                        image.ImageId,
                        InventoryBackupOwnerType.Container));
                    continue;
                }

                if (image.OwnerType == InventoryBackupOwnerType.Item)
                {
                    if (!knownItemIds.Contains(image.OwnerId))
                    {
                        skippedImagesWithMissingOwner++;
                        continue;
                    }

                    var ownership = new InventoryBackupImageOwnership(image.OwnerId, image.ImageId);
                    if (!knownItemImages.Add(ownership))
                    {
                        skippedExistingImages++;
                        continue;
                    }

                    imagesToInsert.Add(new InventoryBackupPlannedImageInsert(
                        image.OwnerId,
                        image.ImageId,
                        InventoryBackupOwnerType.Item));
                    continue;
                }

                skippedImagesWithMissingOwner++;
            }
        }

        var result = new InventoryBackupRestoreResult
        {
            AddedContainers = containersToInsert.Count,
            AddedItems = itemsToInsert.Count,
            AddedRelations = relationsToInsert.Count + relationsToSet.Count,
            AddedRelationQuantity = addedRelationQuantity,
            AddedImages = imagesToInsert.Count,
            UpdatedContainers = containersToUpdate.Count,
            UpdatedItems = itemsToUpdate.Count,
            DeletedContainers = containerIdsToDelete.Count,
            DeletedItems = itemIdsToDelete.Count,
            DeletedRelations = relationsToDelete.Count,
            DeletedImages = imagesToDelete.Count,
            SkippedExistingContainers = skippedExistingContainers,
            SkippedExistingItems = skippedExistingItems,
            SkippedExistingRelations = skippedExistingRelations,
            SkippedExistingImages = skippedExistingImages,
            SkippedInvalidRelations = skippedInvalidRelations,
            SkippedImagesWithMissingOwner = skippedImagesWithMissingOwner,
        };

        return new InventoryBackupRestorePlan(
            containersToInsert,
            containersToUpdate,
            containerIdsToDelete,
            itemsToInsert,
            itemsToUpdate,
            itemIdsToDelete,
            relationsToInsert,
                relationsToSet,
                relationsToDelete,
            imagesToInsert,
                imagesToDelete,
            result);
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
