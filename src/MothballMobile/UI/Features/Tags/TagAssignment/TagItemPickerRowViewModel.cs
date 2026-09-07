using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Domain.Entities.InventoryAggregate;

namespace MothballMobile.UI.Features.Tags.TagAssignment;

/// <summary>Represents an item that can receive a tag.</summary>
public partial class TagItemPickerRowViewModel : ObservableObject
{
    private readonly Func<Guid, Task> select;

    /// <summary>Creates an item row with the action used to assign the selected tag.</summary>
    /// <param name="inventory">The inventory snapshot displayed by the row.</param>
    /// <param name="imagePaths">Resolves the item's local photo paths.</param>
    /// <param name="select">Assigns the tag when the row is selected.</param>
    public TagItemPickerRowViewModel(
        InventorySnapshot inventory,
        IImagePathResolver imagePaths,
        Func<Guid, Task> select)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(imagePaths);
        this.select = select ?? throw new ArgumentNullException(nameof(select));
        ItemId = inventory.Item.ItemId;
        Name = inventory.Item.Name;
        Description = inventory.Item.Description;
        TotalQuantity = inventory.TotalQuantity;
        AssignedQuantity = inventory.AssignedQuantity;
        UnassignedQuantity = inventory.UnassignedQuantity;
        ImagePaths = new(imagePaths.GetItemPhotoPaths(inventory.Item));
    }

    public Guid ItemId { get; }
    public string Name { get; }
    public string Description { get; }
    public int TotalQuantity { get; }
    public int AssignedQuantity { get; }
    public int UnassignedQuantity { get; }
    public ObservableCollection<string> ImagePaths { get; }

    [RelayCommand]
    private Task SelectAsync() => select(ItemId);
}
