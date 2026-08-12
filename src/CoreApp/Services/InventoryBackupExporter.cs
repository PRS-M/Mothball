using System.Text.Json;
using CoreApp.Contracts;
using CoreApp.Interfaces;
using CoreApp.Specifications;

namespace CoreApp.Services;

public sealed class InventoryBackupExporter : IInventoryBackupExporter
{
    private readonly IInventoryQueryRepository inventoryQueries;

    private static readonly JsonSerializerOptions BackupJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public InventoryBackupExporter(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries;
    }

    public async Task<InventoryBackupEnvelope> ExportAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var containers = await inventoryQueries
            .QueryContainersAsync(new ContainerListSpecification(ContainerQueryFilter.All))
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var items = await inventoryQueries
            .QueryItemsWithPhotosAsync(new ItemListSpecification(ItemQueryFilter.All))
            .ConfigureAwait(false);

        var backupContainers = containers
            .Select(c => new InventoryBackupContainer
            {
                ContainerId = c.ContainerId,
                Name = c.Name,
                Notes = c.Notes,
            })
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.ContainerId)
            .ToList();

        var backupItems = items
            .Select(i => new InventoryBackupItem
            {
                ItemId = i.ItemId,
                Name = i.Name,
                Description = i.Description,
            })
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(i => i.ItemId)
            .ToList();

        var backupRelations = containers
            .SelectMany(c => c.Items.Select(stored => new InventoryBackupRelation
            {
                ContainerId = c.ContainerId,
                ItemId = stored.ItemId,
                Quantity = stored.Quantity,
            }))
            .Where(r => r.Quantity > 0)
            .OrderBy(r => r.ContainerId)
            .ThenBy(r => r.ItemId)
            .ToList();

        var backupImages = containers
            .SelectMany(c => c.Photos.Select(photo => new InventoryBackupImageRef
            {
                ImageId = photo.ImageId,
                OwnerId = c.ContainerId,
                OwnerType = InventoryBackupOwnerType.Container,
                FileName = photo.FileName,
            }))
            .Concat(items.SelectMany(i => i.Photos.Select(photo => new InventoryBackupImageRef
            {
                ImageId = photo.ImageId,
                OwnerId = i.ItemId,
                OwnerType = InventoryBackupOwnerType.Item,
                FileName = photo.FileName,
            })))
            .OrderBy(i => i.OwnerType)
            .ThenBy(i => i.OwnerId)
            .ThenBy(i => i.ImageId)
            .ToList();

        return new InventoryBackupEnvelope
        {
            CreatedUtc = DateTimeOffset.UtcNow,
            Data = new InventoryBackupData
            {
                Containers = backupContainers,
                Items = backupItems,
                Relations = backupRelations,
                Images = backupImages,
            },
        };
    }

    public async Task<string> ExportAsJsonAsync(CancellationToken cancellationToken = default)
    {
        var backup = await ExportAsync(cancellationToken).ConfigureAwait(false);
        return JsonSerializer.Serialize(backup, BackupJsonOptions);
    }
}
