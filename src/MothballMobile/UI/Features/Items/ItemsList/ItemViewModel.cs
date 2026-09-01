using CoreApp.Domain.Entities.InventoryAggregate;
﻿using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ItemAggregate;
using System.Threading.Tasks;
using CoreApp.Application.Contracts;

namespace MothballMobile.UI.Features.Items.ItemsList;

public partial class ItemViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService nav;
    private readonly Func<ItemViewModel, Task> editQuantity;
    private readonly Func<ItemViewModel, Task> use;
    private readonly Func<ItemViewModel, Task> delete;

    public ItemViewModel(
        InventorySnapshot inventory,
        IImagePathResolver paths,
        INavigationService nav,
        bool showQuantityManagement,
        Func<ItemViewModel, Task> editQuantity,
        Func<ItemViewModel, Task> use,
        Func<ItemViewModel, Task> delete)
        : base(inventory, paths)
    {
        this.nav = nav;
        ShowQuantityManagement = showQuantityManagement;
        this.editQuantity = editQuantity;
        this.use = use;
        this.delete = delete;
        LoadItemImages();
    }

    public bool ShowQuantityManagement { get; }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return nav.GoToAsync(
            Infrastructure.NavigationRoutes.ItemDetails,
            new Infrastructure.Navigation.ItemDetailsNavigationRequest(Item.ItemId));
    }

    [RelayCommand]
    private Task EditQuantityAsync()
        => ShowQuantityManagement ? editQuantity(this) : Task.CompletedTask;

    [RelayCommand]
    private Task UseAsync()
        => ShowQuantityManagement ? use(this) : Task.CompletedTask;

    [RelayCommand]
    private Task DeleteAsync()
        => delete(this);
}
