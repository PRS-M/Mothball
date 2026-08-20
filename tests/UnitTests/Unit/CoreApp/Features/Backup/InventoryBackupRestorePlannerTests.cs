using CoreApp.Contracts;
using CoreApp.Domain.Inventory;
using CoreApp.Utilities;

namespace Mothball.Tests.Unit.Core.Features.Backup;

[TestFixture]
public class InventoryBackupRestorePlannerTests
{
    [Test]
    public void BuildPlan_InventoryMergePolicy_UsesFormatAgnosticPolicy()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "Old", "Old notes")],
            Items: [new InventoryBackupExistingItem(itemId, "Old item", "Old description")],
            ContainerImages: [],
            ItemImages: [],
            Relations: []);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "New", Notes = "New notes" }],
                Items = [new InventoryBackupItem { ItemId = itemId, Name = "New item", Description = "New description" }],
                Relations = [],
                Images = [],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryMergePolicy.AddAndUpsertMetadata);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContainersToUpdate, Has.Count.EqualTo(1));
            Assert.That(plan.ItemsToUpdate, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void BuildPlan_AddOnly_InsertsMissingAndSkipsExisting()
    {
        var containerId = Guid.NewGuid();
        var newContainerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var newItemId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers:
            [
                new InventoryBackupExistingContainer(containerId, "Existing", "Notes"),
            ],
            Items:
            [
                new InventoryBackupExistingItem(itemId, "Item", "Desc"),
            ],
            ContainerImages: [],
            ItemImages: [],
            Relations:
            [
                new InventoryBackupExistingRelation(containerId, itemId, 2),
            ]);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers =
                [
                    new InventoryBackupContainer { ContainerId = containerId, Name = "Existing", Notes = "Notes" },
                    new InventoryBackupContainer { ContainerId = newContainerId, Name = "New", Notes = "New" },
                ],
                Items =
                [
                    new InventoryBackupItem { ItemId = itemId, Name = "Item", Description = "Desc" },
                    new InventoryBackupItem { ItemId = newItemId, Name = "New Item", Description = "New Desc" },
                ],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = itemId, Quantity = 5 },
                    new InventoryBackupRelation { ContainerId = newContainerId, ItemId = newItemId, Quantity = 1 },
                ],
                Images = [],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.AddOnly);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContainersToInsert.Select(x => x.ContainerId), Is.EquivalentTo(new[] { newContainerId }));
            Assert.That(plan.ItemsToInsert.Select(x => x.ItemId), Is.EquivalentTo(new[] { newItemId }));
            Assert.That(plan.ContainersToUpdate, Is.Empty);
            Assert.That(plan.ItemsToUpdate, Is.Empty);
            Assert.That(plan.RelationsToInsert, Has.Count.EqualTo(2));
            Assert.That(plan.RelationsToSet, Is.Empty);
            Assert.That(plan.RelationsToDelete, Is.Empty);
            Assert.That(plan.Result.SkippedExistingContainers, Is.EqualTo(1));
            Assert.That(plan.Result.SkippedExistingItems, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuildPlan_AddAndUpsertMetadata_UpdatesExistingMetadata()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "Old", "Old Notes")],
            Items: [new InventoryBackupExistingItem(itemId, "Old Item", "Old Desc")],
            ContainerImages: [],
            ItemImages: [],
            Relations: []);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "New", Notes = "New Notes" }],
                Items = [new InventoryBackupItem { ItemId = itemId, Name = "New Item", Description = "New Desc" }],
                Relations = [],
                Images = [],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.AddAndUpsertMetadata);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContainersToUpdate, Has.Count.EqualTo(1));
            Assert.That(plan.ItemsToUpdate, Has.Count.EqualTo(1));
            Assert.That(plan.Result.UpdatedContainers, Is.EqualTo(1));
            Assert.That(plan.Result.UpdatedItems, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuildPlan_FullSync_DeletesMissingRoots_ButKeepsAdditiveChildBehavior()
    {
        var keepContainerId = Guid.NewGuid();
        var deleteContainerId = Guid.NewGuid();
        var keepItemId = Guid.NewGuid();
        var deleteItemId = Guid.NewGuid();

        var keepImageId = Guid.NewGuid();
        var extraExistingImageId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers:
            [
                new InventoryBackupExistingContainer(keepContainerId, "Keep", "K"),
                new InventoryBackupExistingContainer(deleteContainerId, "Delete", "D"),
            ],
            Items:
            [
                new InventoryBackupExistingItem(keepItemId, "Keep Item", "K"),
                new InventoryBackupExistingItem(deleteItemId, "Delete Item", "D"),
            ],
            ContainerImages:
            [
                new InventoryBackupImageOwnership(keepContainerId, keepImageId),
                new InventoryBackupImageOwnership(keepContainerId, extraExistingImageId),
            ],
            ItemImages: [],
            Relations:
            [
                new InventoryBackupExistingRelation(keepContainerId, keepItemId, 5),
                new InventoryBackupExistingRelation(deleteContainerId, deleteItemId, 1),
            ]);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = keepContainerId, Name = "Keep", Notes = "K" }],
                Items = [new InventoryBackupItem { ItemId = keepItemId, Name = "Keep Item", Description = "K" }],
                Relations =
                [
                    // Lower than existing quantity. FullSync keeps additive behavior (no reduction).
                    new InventoryBackupRelation { ContainerId = keepContainerId, ItemId = keepItemId, Quantity = 2 },
                ],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Container,
                        OwnerId = keepContainerId,
                        ImageId = keepImageId,
                        FileName = "keep.jpg",
                    },
                ],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.FullSync);

        Assert.Multiple(() =>
        {
            Assert.That(plan.ContainerIdsToDelete, Is.EquivalentTo(new[] { deleteContainerId }));
            Assert.That(plan.ItemIdsToDelete, Is.EquivalentTo(new[] { deleteItemId }));
            Assert.That(plan.RelationsToInsert, Is.Empty);
            Assert.That(plan.RelationsToDelete, Is.Empty);
            Assert.That(plan.ImagesToDelete, Is.Empty);
            Assert.That(plan.Result.DeletedContainers, Is.EqualTo(1));
            Assert.That(plan.Result.DeletedItems, Is.EqualTo(1));
            Assert.That(plan.Result.DeletedRelations, Is.EqualTo(0));
            Assert.That(plan.Result.DeletedImages, Is.EqualTo(0));
        });
    }

    [Test]
    public void BuildPlan_StrictFullSync_ReconcilesRelationsAndImagesExactly()
    {
        var containerId = Guid.NewGuid();
        var item1Id = Guid.NewGuid();
        var item2Id = Guid.NewGuid();

        var keepImageId = Guid.NewGuid();
        var deleteImageId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "C", "N")],
            Items:
            [
                new InventoryBackupExistingItem(item1Id, "I1", "D1"),
                new InventoryBackupExistingItem(item2Id, "I2", "D2"),
            ],
            ContainerImages:
            [
                new InventoryBackupImageOwnership(containerId, keepImageId),
                new InventoryBackupImageOwnership(containerId, deleteImageId),
            ],
            ItemImages: [],
            Relations:
            [
                new InventoryBackupExistingRelation(containerId, item1Id, 5),
                new InventoryBackupExistingRelation(containerId, item2Id, 1),
            ]);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "C", Notes = "N" }],
                Items =
                [
                    new InventoryBackupItem { ItemId = item1Id, Name = "I1", Description = "D1" },
                    new InventoryBackupItem { ItemId = item2Id, Name = "I2", Description = "D2" },
                ],
                Relations = [new InventoryBackupRelation { ContainerId = containerId, ItemId = item1Id, Quantity = 2 }],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Container,
                        OwnerId = containerId,
                        ImageId = keepImageId,
                        FileName = "keep.jpg",
                    },
                ],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.StrictFullSync);

        Assert.Multiple(() =>
        {
            Assert.That(plan.RelationsToSet, Has.Count.EqualTo(1));
            Assert.That(plan.RelationsToSet.Single().Quantity, Is.EqualTo(2));
            Assert.That(plan.RelationsToDelete, Has.Count.EqualTo(1));
            Assert.That(plan.RelationsToDelete.Single().ItemId, Is.EqualTo(item2Id));
            Assert.That(plan.ImagesToDelete, Has.Count.EqualTo(1));
            Assert.That(plan.ImagesToDelete.Single().ImageId, Is.EqualTo(deleteImageId));
            Assert.That(plan.Result.DeletedRelations, Is.EqualTo(1));
            Assert.That(plan.Result.DeletedImages, Is.EqualTo(1));
        });
    }

    [Test]
    public void BuildPlan_StrictFullSync_AggregatesDuplicateRelations_AndTracksAddedQuantity()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "C", "N")],
            Items: [new InventoryBackupExistingItem(itemId, "I", "D")],
            ContainerImages: [],
            ItemImages: [],
            Relations: [new InventoryBackupExistingRelation(containerId, itemId, 2)]);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "C", Notes = "N" }],
                Items = [new InventoryBackupItem { ItemId = itemId, Name = "I", Description = "D" }],
                Relations =
                [
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = itemId, Quantity = 2 },
                    new InventoryBackupRelation { ContainerId = containerId, ItemId = itemId, Quantity = 3 },
                ],
                Images = [],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.StrictFullSync);

        Assert.Multiple(() =>
        {
            Assert.That(plan.RelationsToSet, Has.Count.EqualTo(1));
            Assert.That(plan.RelationsToSet.Single().Quantity, Is.EqualTo(5));
            Assert.That(plan.Result.AddedRelationQuantity, Is.EqualTo(3));
            Assert.That(plan.Result.SkippedExistingRelations, Is.EqualTo(0));
            Assert.That(plan.RelationsToDelete, Is.Empty);
        });
    }

    [Test]
    public void BuildPlan_StrictFullSync_SkipsEqualRelation_AndDeletesMissingPairs()
    {
        var containerId = Guid.NewGuid();
        var keepItemId = Guid.NewGuid();
        var deleteItemId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "C", "N")],
            Items:
            [
                new InventoryBackupExistingItem(keepItemId, "Keep", "D"),
                new InventoryBackupExistingItem(deleteItemId, "Delete", "D"),
            ],
            ContainerImages: [],
            ItemImages: [],
            Relations:
            [
                new InventoryBackupExistingRelation(containerId, keepItemId, 4),
                new InventoryBackupExistingRelation(containerId, deleteItemId, 1),
            ]);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "C", Notes = "N" }],
                Items =
                [
                    new InventoryBackupItem { ItemId = keepItemId, Name = "Keep", Description = "D" },
                    new InventoryBackupItem { ItemId = deleteItemId, Name = "Delete", Description = "D" },
                ],
                Relations = [new InventoryBackupRelation { ContainerId = containerId, ItemId = keepItemId, Quantity = 4 }],
                Images = [],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.StrictFullSync);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Result.SkippedExistingRelations, Is.EqualTo(1));
            Assert.That(plan.RelationsToSet, Is.Empty);
            Assert.That(plan.RelationsToDelete, Has.Count.EqualTo(1));
            Assert.That(plan.RelationsToDelete.Single().ItemId, Is.EqualTo(deleteItemId));
        });
    }

    [Test]
    public void BuildPlan_StrictFullSync_ReconcilesContainerAndItemImages_WithSkipAndDeleteCounts()
    {
        var containerId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var keepContainerImageId = Guid.NewGuid();
        var deleteContainerImageId = Guid.NewGuid();
        var insertContainerImageId = Guid.NewGuid();

        var keepItemImageId = Guid.NewGuid();
        var deleteItemImageId = Guid.NewGuid();
        var insertItemImageId = Guid.NewGuid();

        var existing = new InventoryBackupExistingState(
            Containers: [new InventoryBackupExistingContainer(containerId, "C", "N")],
            Items: [new InventoryBackupExistingItem(itemId, "I", "D")],
            ContainerImages:
            [
                new InventoryBackupImageOwnership(containerId, keepContainerImageId),
                new InventoryBackupImageOwnership(containerId, deleteContainerImageId),
            ],
            ItemImages:
            [
                new InventoryBackupImageOwnership(itemId, keepItemImageId),
                new InventoryBackupImageOwnership(itemId, deleteItemImageId),
            ],
            Relations: []);

        var backup = new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData
            {
                Containers = [new InventoryBackupContainer { ContainerId = containerId, Name = "C", Notes = "N" }],
                Items = [new InventoryBackupItem { ItemId = itemId, Name = "I", Description = "D" }],
                Relations = [],
                Images =
                [
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Container,
                        OwnerId = containerId,
                        ImageId = keepContainerImageId,
                        FileName = "keep-c.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Container,
                        OwnerId = containerId,
                        ImageId = insertContainerImageId,
                        FileName = "insert-c.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Item,
                        OwnerId = itemId,
                        ImageId = keepItemImageId,
                        FileName = "keep-i.jpg",
                    },
                    new InventoryBackupImageRef
                    {
                        OwnerType = InventoryBackupOwnerType.Item,
                        OwnerId = itemId,
                        ImageId = insertItemImageId,
                        FileName = "insert-i.jpg",
                    },
                ],
            },
        };

        var plan = InventoryBackupRestorePlanner.BuildPlan(backup, existing, InventoryBackupConflictPolicy.StrictFullSync);

        Assert.Multiple(() =>
        {
            Assert.That(plan.Result.SkippedExistingImages, Is.EqualTo(2));
            Assert.That(plan.ImagesToInsert, Has.Count.EqualTo(2));
            Assert.That(plan.ImagesToDelete, Has.Count.EqualTo(2));
            Assert.That(plan.ImagesToDelete.Select(x => x.ImageId), Is.EquivalentTo(new[]
            {
                deleteContainerImageId,
                deleteItemImageId,
            }));
        });
    }

    [Test]
    public void BuildPlan_UnsupportedConflictPolicy_ThrowsNotSupportedException()
    {
        var backup = new InventoryBackupEnvelope { Data = new InventoryBackupData() };
        var existing = new InventoryBackupExistingState([], [], [], [], []);

        Assert.Throws<NotSupportedException>(() => InventoryBackupRestorePlanner.BuildPlan(
            backup,
            existing,
            (InventoryBackupConflictPolicy)999));
    }

    [Test]
    public void ValidateIntegrity_WhenMissingAndRequired_Throws()
    {
        var backup = new InventoryBackupEnvelope
        {
            Integrity = new InventoryBackupIntegrity { PayloadChecksum = string.Empty },
            Data = new InventoryBackupData(),
        };

        var options = new InventoryBackupRestoreOptions { RequireIntegrityValidation = true };

        Assert.Throws<InvalidDataException>(() => InventoryBackupRestorePlanner.ValidateIntegrity(backup, options));
    }

    [Test]
    public void ValidateIntegrity_WhenMissingAndOptional_DoesNotThrow()
    {
        var backup = new InventoryBackupEnvelope
        {
            Integrity = new InventoryBackupIntegrity { PayloadChecksum = string.Empty },
            Data = new InventoryBackupData(),
        };

        var options = new InventoryBackupRestoreOptions { RequireIntegrityValidation = false };

        Assert.DoesNotThrow(() => InventoryBackupRestorePlanner.ValidateIntegrity(backup, options));
    }

    [Test]
    public void ValidateIntegrity_WhenSignatureMetadataIncomplete_Throws()
    {
        var signed = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData(),
        }, signatureSecret: "secret");

        var broken = signed with
        {
            Integrity = signed.Integrity with
            {
                SignatureAlgorithm = null,
            },
        };

        Assert.Throws<InvalidDataException>(() => InventoryBackupRestorePlanner.ValidateIntegrity(
            broken,
            new InventoryBackupRestoreOptions { SignatureSecret = "secret" }));
    }

    [Test]
    public void ValidateIntegrity_WhenSignaturePresentAndSecretMissing_Throws()
    {
        var signed = InventoryBackupRestorePlanner.AttachIntegrity(new InventoryBackupEnvelope
        {
            Data = new InventoryBackupData(),
        }, signatureSecret: "secret");

        Assert.Throws<InvalidDataException>(() => InventoryBackupRestorePlanner.ValidateIntegrity(
            signed,
            new InventoryBackupRestoreOptions { SignatureSecret = null }));
    }
}
