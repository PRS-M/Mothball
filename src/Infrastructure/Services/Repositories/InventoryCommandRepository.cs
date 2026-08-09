using CoreApp.Entities.ContainerAggregate;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Entities.Shared;
using CoreApp.Interfaces;
using Infrastructure.Interfaces;

namespace Infrastructure.Services.Repositories;

/// <summary>
/// Command-side inventory repository composed from focused repositories.
/// </summary>
public class InventoryCommandRepository : IInventoryCommandRepository
{
    private readonly IContainerRepository containerRepo;
    private readonly IItemRepository itemRepo;
    private readonly IImageRepository imageRepo;
    private readonly IRelationRepository relationRepo;

    public InventoryCommandRepository(
        IContainerRepository containerRepo,
        IItemRepository itemRepo,
        IImageRepository imageRepo,
        IRelationRepository relationRepo)
    {
        this.containerRepo = containerRepo;
        this.itemRepo = itemRepo;
        this.imageRepo = imageRepo;
        this.relationRepo = relationRepo;
    }

    public Task InsertContainerAsync(Container container)
        => containerRepo.InsertAsync(container);

    public Task InsertItemAsync(Item item)
        => itemRepo.InsertAsync(item);

    public Task InsertImageItemAsync(ImageItem imageItem, Guid ownerId)
        => imageRepo.InsertAsync(imageItem, ownerId);

    public Task InsertItemContainerRelation(Guid itemId, Guid containerId, int quantity)
        => relationRepo.InsertItemContainerRelationAsync(itemId, containerId, quantity);

    public Task UpdateContainerAsync(Container container)
        => containerRepo.UpdateAsync(container);

    public Task UpdateItemAsync(Item item)
        => itemRepo.UpdateAsync(item);

    public Task UpdateImageItemAsync(ImageItem image, Guid ownerId)
        => imageRepo.UpdateAsync(image, ownerId);

    public Task DeleteItemAsync(string itemId)
        => itemRepo.DeleteAsync(itemId);

    public Task DeleteContainerAsync(string containerId)
        => containerRepo.DeleteAsync(containerId);
}
