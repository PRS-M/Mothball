using CoreApp.Entities.Inventory;
﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Contracts;

namespace MothballMobile.UI.Shared;

public abstract class ItemWithImagesViewModelBase : ObservableObject
{
    protected ItemWithImagesViewModelBase(InventorySnapshot inventory, IImagePathResolver paths)
    {
        Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        this.paths = paths;
    }

    private readonly IImagePathResolver paths;

    public InventorySnapshot Inventory { get; }
    public Item Item => Inventory.Item;

    public string Name => Item.Name;
    public string Description => Item.Description;
    public int TotalQuantity => Inventory.TotalQuantity;
    public int AssignedQuantity => Inventory.AssignedQuantity;
    public int UnassignedQuantity => Inventory.UnassignedQuantity;

    public ObservableCollection<string> ImagePaths { get; } = new();

    protected Task LoadItemImagesAsync(bool clearFirst = true)
    {
        if (clearFirst)
        {
            ImagePaths.Clear();
        }

        foreach (var imagePath in paths.GetItemPhotoPaths(Item))
        {
            ImagePaths.Add(imagePath);
        }

        return Task.CompletedTask;
    }
}
