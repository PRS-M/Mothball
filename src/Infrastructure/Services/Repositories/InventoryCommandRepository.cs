using CoreApp.Domain.Entities.InventoryAggregate;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.ItemAggregate;
using CoreApp.Domain.ValueObjects;
using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Command-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryCommandRepository : IInventoryCommandRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IItemInventoryRepository itemInventoryRepo;
    private readonly IImageRepository imageRepo;
    private readonly IRelationRepository relationRepo;
    private readonly IDomainEventDispatcher? domainEvents;
    private readonly IInventoryChangeTracker? inventoryChanges;

    public InventoryCommandRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IItemInventoryRepository itemInventoryRepo,
        IImageRepository imageRepo,
        IRelationRepository relationRepo,
        IInventoryChangeTracker? inventoryChanges = null,
        IDomainEventDispatcher? domainEvents = null)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.itemInventoryRepo = itemInventoryRepo;
        this.imageRepo = imageRepo;
        this.relationRepo = relationRepo;
        this.domainEvents = domainEvents;
        this.inventoryChanges = inventoryChanges;
    }

    /// <inheritdoc />
    public Task InsertContainerAsync(Container container)
        => PersistFromSourcesAsync(() => containerRepo.InsertAsync(container), container);

    /// <inheritdoc />
    public Task InsertItemAsync(Item item)
        => PersistFromSourcesAsync(() => itemRepo.InsertAsync(item), item);

    /// <inheritdoc />
    public Task InsertItemInventoryAsync(ItemInventory inventory)
        => PersistFromSourcesAsync(() => itemInventoryRepo.InsertAsync(inventory), inventory);

    /// <inheritdoc />
    public Task SaveItemInventoryAsync(ItemInventory inventory)
        => PersistFromSourcesAsync(() => itemInventoryRepo.SaveAsync(inventory), inventory);

    /// <inheritdoc />
    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => PersistWithEventsAsync(() => imageRepo.InsertAsync(imageItem, ownerId));

    /// <inheritdoc />
    public async Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        int existingQuantity = inventory.Allocations
            .FirstOrDefault(allocation => allocation.ContainerId == containerId)?.Quantity ?? 0;
        inventory.SetContainerAllocation(containerId, string.Empty, existingQuantity + quantity);
        await PersistFromSourcesAsync(() => itemInventoryRepo.SaveAsync(inventory), inventory);
    }

    /// <inheritdoc />
    public async Task ReplaceItemContainerRelationQuantity(Guid itemId, Guid containerId, int quantity)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId)
            ?? new ItemInventory(itemId, Math.Max(1, quantity));
        inventory.SetContainerAllocation(containerId, string.Empty, quantity);
        await PersistFromSourcesAsync(() => itemInventoryRepo.SaveAsync(inventory), inventory);
    }

    /// <inheritdoc />
    public Task SetItemContainerAllocationAsync(Item item, Guid containerId, int quantity)
        => PersistFromSourcesAsync(() => relationRepo.SetItemContainerAllocationAsync(item, containerId, quantity), item);

    /// <inheritdoc />
    public Task ApplyItemInventoryWithdrawalAsync(
        Item item,
        IReadOnlyCollection<CoreApp.Domain.Entities.InventoryAggregate.ItemContainerAllocation> allocations)
        => PersistFromSourcesAsync(() => relationRepo.ApplyItemInventoryWithdrawalAsync(item, allocations), item);

    /// <inheritdoc />
    public async Task DeleteItemContainerRelation(Guid itemId, Guid containerId)
    {
        var inventory = await itemInventoryRepo.GetAsync(itemId);
        if (inventory is null)
        {
            await relationRepo.DeleteItemContainerRelationAsync(itemId, containerId);
            await PublishFallbackChangeAsync();

            return;
        }

        inventory.SetContainerAllocation(containerId, string.Empty, 0);
        await PersistFromSourcesAsync(() => itemInventoryRepo.SaveAsync(inventory), inventory);
    }

    /// <inheritdoc />
    public Task UpdateContainerAsync(Container container)
        => PersistFromSourcesAsync(() => containerRepo.UpdateAsync(container), container);

    /// <inheritdoc />
    public Task UpdateItemAsync(Item item)
        => PersistFromSourcesAsync(() => itemRepo.UpdateAsync(item), item);

    /// <inheritdoc />
    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => PersistWithEventsAsync(() => imageRepo.UpdateAsync(image, ownerId));

    /// <inheritdoc />
    public Task DeleteImageItemAsync(Guid imageId, Guid ownerId)
        => PersistWithEventsAsync(() => imageRepo.DeleteAsync(imageId, ownerId));

    /// <inheritdoc />
    public Task DeleteContainerPhotoAsync(Container container, Guid imageId)
        => PersistFromSourcesAsync(() => containerRepo.DeletePhotoAsync(container, imageId), container);

    /// <inheritdoc />
    public Task DeleteItemPhotoAsync(Item item, Guid imageId)
        => PersistFromSourcesAsync(() => itemRepo.DeletePhotoAsync(item, imageId), item);

    /// <inheritdoc />
    public Task DeleteItemAsync(string itemId)
        => PersistWithEventsAsync(
            () => itemRepo.DeleteAsync(itemId),
            CreateDeletionEvent<ItemDeleted>(itemId, id => new ItemDeleted(id)));

    /// <inheritdoc />
    public Task DeleteContainerAsync(string containerId)
        => PersistWithEventsAsync(
            () => containerRepo.DeleteAsync(containerId),
            CreateDeletionEvent<ContainerDeleted>(containerId, id => new ContainerDeleted(id)));

    private async Task PersistFromSourcesAsync(
        Func<Task> mutation,
        params IDomainEventSource[] sources)
    {
        await mutation().ConfigureAwait(false);
        var events = sources
            .SelectMany(source => source.DomainEvents)
            .ToArray();

        if (domainEvents is not null && events.Length > 0)
        {
            await domainEvents.DispatchAsync(events).ConfigureAwait(false);
        }

        foreach (var source in sources)
        {
            source.ClearDomainEvents();
        }

        if (events.Length == 0 || domainEvents is null)
        {
            inventoryChanges?.MarkChanged();
        }
    }

    private async Task PersistWithEventsAsync(
        Func<Task> mutation,
        params IDomainEvent[] events)
    {
        await mutation().ConfigureAwait(false);
        if (domainEvents is not null && events.Length > 0)
        {
            await domainEvents.DispatchAsync(events).ConfigureAwait(false);
        }

        if (events.Length == 0 || domainEvents is null)
        {
            inventoryChanges?.MarkChanged();
        }
    }

    private async Task PublishFallbackChangeAsync()
    {
        if (domainEvents is null)
        {
            inventoryChanges?.MarkChanged();
        }
        else
        {
            await domainEvents.DispatchAsync(
                [new InventoryChanged(Guid.Empty, 0, 0, 0, 0, [])]).ConfigureAwait(false);
        }
    }

    private static TEvent[] CreateDeletionEvent<TEvent>(
        string identifier,
        Func<Guid, TEvent> factory)
        where TEvent : IDomainEvent
    {
        return Guid.TryParse(identifier, out var id) && id != Guid.Empty
            ? [factory(id)]
            : [];
    }
}
