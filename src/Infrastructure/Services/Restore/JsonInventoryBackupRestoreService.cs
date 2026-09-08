using Infrastructure.Services.JsonStore;
using Infrastructure.Services.JsonStore.Models;
using CoreApp.Application.Contracts.Backup;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Features.Barcodes.Commands;
using CoreApp.Application.Contracts;

namespace Infrastructure.Services.Restore;

public sealed class JsonInventoryBackupRestoreService : IInventoryBackupRestoreService
{
    private readonly JsonInventoryStore store;
    private readonly IInventoryChangeTracker? inventoryChanges;

    public JsonInventoryBackupRestoreService(
        JsonInventoryStore store,
        IInventoryChangeTracker? inventoryChanges = null)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.inventoryChanges = inventoryChanges;
    }

    /// <inheritdoc />
    public async Task<InventoryBackupRestoreResult> RestoreFromJsonAsync(
        string backupJson,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var backup = InventoryBackupRestorePlanner.ParseBackupJson(backupJson);
        return await RestoreAsync(backup, options, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InventoryBackupRestoreResult> RestoreAsync(
        InventoryBackupEnvelope backup,
        InventoryBackupRestoreOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new InventoryBackupRestoreOptions();

        ArgumentNullException.ThrowIfNull(backup);
        ArgumentNullException.ThrowIfNull(backup.Data);
        InventoryBackupRestorePlanner.ValidatePayloadVersion(backup);
        InventoryBackupRestorePlanner.ValidateIntegrity(backup, options);

        cancellationToken.ThrowIfCancellationRequested();
        InventoryBackupRestoreResult result = new();

        await store.UpdateAsync(state =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var existingState = CreateExistingState(state);
            var plan = InventoryBackupRestorePlanner.BuildPlan(
                backup,
                existingState,
                options.ConflictPolicy,
                options.OverwriteExistingQuantities);
            ApplyPlan(state, plan, cancellationToken);
            SyncBarcodeRegistry(state);
            ApplyTags(state, backup.Data, cancellationToken);
            result = plan.Result;

            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);

        inventoryChanges?.MarkChanged();
        return result;
    }

    private static void SyncBarcodeRegistry(JsonInventoryStore.StoreState state)
    {
        state.Barcodes.RemoveAll(barcode => barcode.Status == (int)BarcodeRegistryStatus.Assigned);

        foreach (var container in state.Containers.Where(container => !string.IsNullOrWhiteSpace(container.BarcodeValue)))
        {
            var value = container.BarcodeValue.Trim();
            state.Barcodes.RemoveAll(barcode => barcode.NormalizedValue == value);
            state.Barcodes.Add(new JsonBarcodeRegistryRow
            {
                BarcodeId = Guid.NewGuid(),
                Value = value,
                NormalizedValue = value,
                Symbology = container.BarcodeSymbology ?? (int)BarcodeSymbology.Code128,
                Status = (int)BarcodeRegistryStatus.Assigned,
                OwnerKind = (int)BarcodeOwnerKind.Container,
                OwnerId = container.ContainerId,
                OwnerName = container.Name,
            });
        }

        foreach (var item in state.Items.Where(item => !string.IsNullOrWhiteSpace(item.BarcodeValue)))
        {
            var value = item.BarcodeValue.Trim();
            state.Barcodes.RemoveAll(barcode => barcode.NormalizedValue == value);
            state.Barcodes.Add(new JsonBarcodeRegistryRow
            {
                BarcodeId = Guid.NewGuid(),
                Value = value,
                NormalizedValue = value,
                Symbology = item.BarcodeSymbology ?? (int)BarcodeSymbology.Code128,
                Status = (int)BarcodeRegistryStatus.Assigned,
                OwnerKind = (int)BarcodeOwnerKind.Item,
                OwnerId = item.ItemId,
                OwnerName = item.Name,
            });
        }
    }

    private static void ApplyTags(
        JsonInventoryStore.StoreState state,
        InventoryBackupData data,
        CancellationToken cancellationToken)
    {
        var tagIdMap = new Dictionary<Guid, Guid>();

        foreach (var backupTag in data.Tags)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (backupTag.TagId == Guid.Empty || string.IsNullOrWhiteSpace(backupTag.Name))
            {
                continue;
            }

            var tagName = new TagName(backupTag.Name);
            var existing = state.Tags.FirstOrDefault(tag => tag.NormalizedName == tagName.NormalizedValue);
            if (existing is null)
            {
                existing = new JsonTagRow
                {
                    TagId = backupTag.TagId,
                    Name = tagName.Value,
                    NormalizedName = tagName.NormalizedValue,
                };
                state.Tags.Add(existing);
            }

            tagIdMap[backupTag.TagId] = existing.TagId;
        }

        var itemIds = state.Items.Select(item => item.ItemId).ToHashSet();
        var containerIds = state.Containers.Select(container => container.ContainerId).ToHashSet();
        state.TagAssignments.RemoveAll(assignment =>
            (assignment.TargetType == TagTargetType.Item && !itemIds.Contains(assignment.TargetId))
            || (assignment.TargetType == TagTargetType.Container && !containerIds.Contains(assignment.TargetId)));

        foreach (var assignment in data.TagAssignments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!tagIdMap.TryGetValue(assignment.TagId, out var tagId)
                || !IsValidTarget(assignment, itemIds, containerIds))
            {
                continue;
            }

            if (state.TagAssignments.Any(existing =>
                    existing.TagId == tagId
                    && existing.TargetId == assignment.TargetId
                    && existing.TargetType == assignment.TargetType))
            {
                continue;
            }

            state.TagAssignments.Add(new JsonTagAssignmentRow
            {
                Id = state.Metadata.NextTagAssignmentId++,
                TagId = tagId,
                TargetId = assignment.TargetId,
                TargetType = assignment.TargetType,
            });
        }
    }

    private static bool IsValidTarget(
        InventoryBackupTagAssignment assignment,
        IReadOnlySet<Guid> itemIds,
        IReadOnlySet<Guid> containerIds)
        => assignment.TargetType switch
        {
            TagTargetType.Item => itemIds.Contains(assignment.TargetId),
            TagTargetType.Container => containerIds.Contains(assignment.TargetId),
            _ => false,
        };

    private static InventoryBackupExistingState CreateExistingState(JsonInventoryStore.StoreState state)
    {
        var containerIds = state.Containers.Select(c => c.ContainerId).ToHashSet();
        var itemIds = state.Items.Select(i => i.ItemId).ToHashSet();

        var containerImages = state.Images
            .Where(image => containerIds.Contains(image.OwnerUniqueId))
            .Select(image => new InventoryBackupImageOwnership(image.OwnerUniqueId, image.ImageId))
            .ToList();

        var itemImages = state.Images
            .Where(image => itemIds.Contains(image.OwnerUniqueId))
            .Select(image => new InventoryBackupImageOwnership(image.OwnerUniqueId, image.ImageId))
            .ToList();
        return new InventoryBackupExistingState(
            state.Containers
                .Select(container => new InventoryBackupExistingContainer(
                    container.ContainerId,
                    container.Name,
                    container.Notes,
                    container.BarcodeValue,
                    container.BarcodeSymbology))
                .ToList(),
            state.Items
                .Select(item => new InventoryBackupExistingItem(
                    item.ItemId,
                    item.Name,
                    item.Description,
                    item.BarcodeValue,
                    item.BarcodeSymbology,
                    state.Inventories.FirstOrDefault(i => i.ItemId == item.ItemId)?.TotalQuantity ?? 1))
                .ToList(),
            containerImages,
            itemImages,
            state.Relations
                .Select(relation => new InventoryBackupExistingRelation(relation.ContainerId, relation.ItemId, relation.Quantity))
                .ToList());
    }

    private static void ApplyPlan(
        JsonInventoryStore.StoreState state,
        InventoryBackupRestorePlan plan,
        CancellationToken cancellationToken)
    {
        foreach (var container in plan.ContainersToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Containers.Add(new JsonContainerRow
            {
                RowId = state.Metadata.NextContainerRowId++,
                ContainerId = container.ContainerId,
                Name = container.Name,
                Notes = container.Notes,
                BarcodeValue = container.BarcodeValue,
                BarcodeSymbology = container.BarcodeSymbology,
            });
        }

        foreach (var container in plan.ContainersToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = state.Containers.FirstOrDefault(c => c.ContainerId == container.ContainerId);
            if (existing is null)
            {
                state.Containers.Add(new JsonContainerRow
                {
                    RowId = state.Metadata.NextContainerRowId++,
                    ContainerId = container.ContainerId,
                    Name = container.Name,
                    Notes = container.Notes,
                    BarcodeValue = container.BarcodeValue,
                    BarcodeSymbology = container.BarcodeSymbology,
                });
                continue;
            }

            existing.Name = container.Name;
            existing.Notes = container.Notes;
            existing.BarcodeValue = container.BarcodeValue;
            existing.BarcodeSymbology = container.BarcodeSymbology;
        }

        foreach (var item in plan.ItemsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Items.Add(new JsonItemRow
            {
                RowId = state.Metadata.NextItemRowId++,
                ItemId = item.ItemId,
                Name = item.Name,
                Description = item.Description,
                BarcodeValue = item.BarcodeValue,
                BarcodeSymbology = item.BarcodeSymbology,
            });
            UpsertInventory(state, item.ItemId, item.TotalQuantity);
        }

        foreach (var item in plan.ItemsToUpdate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var existing = state.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (existing is null)
            {
                state.Items.Add(new JsonItemRow
                {
                    RowId = state.Metadata.NextItemRowId++,
                    ItemId = item.ItemId,
                    Name = item.Name,
                    Description = item.Description,
                    BarcodeValue = item.BarcodeValue,
                    BarcodeSymbology = item.BarcodeSymbology,
                });
                if (plan.ItemIdsWithQuantityOverwrite.Contains(item.ItemId))
                {
                    UpsertInventory(state, item.ItemId, item.TotalQuantity);
                }

                continue;
            }

            if (plan.ItemIdsWithMetadataUpdate.Contains(item.ItemId))
            {
                existing.Name = item.Name;
                existing.Description = item.Description;
                existing.BarcodeValue = item.BarcodeValue;
                existing.BarcodeSymbology = item.BarcodeSymbology;
            }

            if (plan.ItemIdsWithQuantityOverwrite.Contains(item.ItemId))
            {
                UpsertInventory(state, item.ItemId, item.TotalQuantity);
            }
        }

        foreach (var relation in plan.RelationsToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InsertOrIncreaseRelation(
                state,
                relation.ItemId,
                relation.ContainerId,
                relation.QuantityToInsert);
        }

        foreach (var relation in plan.RelationsToSet)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Relations.RemoveAll(r => r.ItemId == relation.ItemId && r.ContainerId == relation.ContainerId);

            if (relation.Quantity > 0)
            {
                state.Relations.Add(new JsonRelationRow
                {
                    Id = state.Metadata.NextRelationId++,
                    ItemId = relation.ItemId,
                    ContainerId = relation.ContainerId,
                    Quantity = relation.Quantity,
                });
            }
        }

        foreach (var relation in plan.RelationsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Relations.RemoveAll(r => r.ItemId == relation.ItemId && r.ContainerId == relation.ContainerId);
        }

        foreach (var image in plan.ImagesToInsert)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Images.Add(new JsonImageRow
            {
                RowId = state.Metadata.NextImageRowId++,
                ImageId = image.ImageId,
                OwnerUniqueId = image.OwnerId,
                ImageDataBase64 = null,
            });
        }

        foreach (var image in plan.ImagesToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Images.RemoveAll(i => i.ImageId == image.ImageId && i.OwnerUniqueId == image.OwnerId);
        }

        foreach (var itemId in plan.ItemIdsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Images.RemoveAll(image => image.OwnerUniqueId == itemId);
            state.Relations.RemoveAll(relation => relation.ItemId == itemId);
            state.Inventories.RemoveAll(inventory => inventory.ItemId == itemId);
            state.Items.RemoveAll(item => item.ItemId == itemId);
        }

        foreach (var containerId in plan.ContainerIdsToDelete)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state.Images.RemoveAll(image => image.OwnerUniqueId == containerId);
            state.Relations.RemoveAll(relation => relation.ContainerId == containerId);
            state.Containers.RemoveAll(container => container.ContainerId == containerId);
        }
    }

    private static void UpsertInventory(JsonInventoryStore.StoreState state, Guid itemId, int totalQuantity)
    {
        var existing = state.Inventories.FirstOrDefault(inventory => inventory.ItemId == itemId);
        if (existing is null)
        {
            state.Inventories.Add(new JsonInventoryRow
            {
                ItemId = itemId,
                TotalQuantity = totalQuantity,
            });

            return;
        }

        existing.TotalQuantity = totalQuantity;
    }

    private static void InsertOrIncreaseRelation(
        JsonInventoryStore.StoreState state,
        Guid itemId,
        Guid containerId,
        int quantity)
    {
        var existingQuantity = state.Relations
            .Where(relation => relation.ItemId == itemId && relation.ContainerId == containerId)
            .Sum(relation => relation.Quantity);

        state.Relations.RemoveAll(relation => relation.ItemId == itemId && relation.ContainerId == containerId);
        state.Relations.Add(new JsonRelationRow
        {
            Id = state.Metadata.NextRelationId++,
            ItemId = itemId,
            ContainerId = containerId,
            Quantity = existingQuantity + quantity,
        });
    }
}
