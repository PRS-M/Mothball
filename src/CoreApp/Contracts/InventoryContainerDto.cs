namespace CoreApp.Contracts;

internal sealed record InventoryContainerDto(
    Guid ContainerId,
    string Name,
    string Notes,
    List<InventoryImageDto>? Photos,
    List<InventoryStoredItemDto>? Items)
{
    public int ItemCount => Items?.Sum(i => i.Quantity) ?? 0;
}

internal sealed record InventoryImageDto(Guid ImageId);

internal sealed record InventoryStoredItemDto(Guid ItemId, int Quantity);
