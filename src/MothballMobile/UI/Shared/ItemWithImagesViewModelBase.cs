using CoreApp.Domain.Entities.InventoryAggregate;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Domain.Entities.ItemAggregate;

namespace MothballMobile.UI.Shared;

public abstract class ItemWithImagesViewModelBase : ObservableObject
{
    protected ItemWithImagesViewModelBase(InventorySnapshot inventory, IImagePathResolver paths)
    {
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.paths = paths;
        totalQuantity = inventory.TotalQuantity;
        assignedQuantity = inventory.AssignedQuantity;
        unassignedQuantity = inventory.UnassignedQuantity;
    }

    private readonly IImagePathResolver paths;
    private int totalQuantity;
    private int assignedQuantity;
    private int unassignedQuantity;

    public InventorySnapshot Inventory { get; }
    public Item Item => Inventory.Item;

    public string Name => Item.Name;
    public string Description => Item.Description;
    public int TotalQuantity => totalQuantity;
    public int AssignedQuantity => assignedQuantity;
    public int UnassignedQuantity => unassignedQuantity;

    public ObservableCollection<string> ImagePaths { get; } = new();

    /// <summary>
    /// Refreshes the total/assigned/unassigned quantities after a save that changes global allocation.
    /// </summary>
    public void UpdateQuantities(int total, int assigned, int unassigned)
    {
        SetProperty(ref totalQuantity, total, nameof(TotalQuantity));
        SetProperty(ref assignedQuantity, assigned, nameof(AssignedQuantity));
        SetProperty(ref unassignedQuantity, unassigned, nameof(UnassignedQuantity));
    }

    /// <summary>
    /// Loads this item's photo paths into <see cref="ImagePaths"/>.
    /// </summary>
    /// <param name="clearFirst">Whether to clear existing paths before loading.</param>
    protected void LoadItemImages(bool clearFirst = true)
    {
        if (clearFirst)
        {
            ImagePaths.Clear();
        }

        foreach (var imagePath in paths.GetItemPhotoPaths(Item))
        {
            ImagePaths.Add(imagePath);
        }
    }

    protected Task LoadItemImagesAsync(bool clearFirst = true)
    {
        LoadItemImages(clearFirst);
        return Task.CompletedTask;
    }
}
