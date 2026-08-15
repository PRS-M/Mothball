using CoreApp.Entities.Inventory;
﻿using CommunityToolkit.Mvvm.Input;
using CoreApp.Entities.ItemAggregate;
using CoreApp.Interfaces;
using CoreApp.Contracts;

namespace MothballMobile.UI.Features.Containers.AddExistingItemToContainer;

public partial class UnassignedItemViewModel : ItemWithImagesViewModelBase
{
    private readonly Func<Guid, Task> assign;

    public UnassignedItemViewModel(
        InventorySnapshot inventory,
        IImagePathResolver paths,
        Func<Guid, Task> assign,
        bool showQuantityManagement)
        : base(inventory, paths)
    {
        this.assign = assign;
        ShowQuantityManagement = showQuantityManagement;
    }

    public bool ShowQuantityManagement { get; }

    public Task LoadImagesAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task AssignToContainerAsync() => assign(Item.ItemId);
}
