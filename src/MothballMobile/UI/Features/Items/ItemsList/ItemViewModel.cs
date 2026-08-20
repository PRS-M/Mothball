using CoreApp.Domain.Entities.InventoryAggregate;
﻿using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ItemAggregate;
using System.Threading.Tasks;
using CoreApp.Application.Contracts;

namespace MothballMobile.UI.Features.Items.ItemsList;

public partial class ItemViewModel : ItemWithImagesViewModelBase
{
    private readonly INavigationService nav;

    public ItemViewModel(
        InventorySnapshot inventory,
        IImagePathResolver paths,
        INavigationService nav,
        bool showQuantityManagement)
        : base(inventory, paths)
    {
        this.nav = nav;
        ShowQuantityManagement = showQuantityManagement;
    }

    public bool ShowQuantityManagement { get; }

    public Task LoadImageAsync()
    {
        return LoadItemImagesAsync(clearFirst: true);
    }

    [RelayCommand]
    private Task NavigateToItemDetailsAsync()
    {
        return nav.GoToAsync(Infrastructure.NavigationRoutes.ItemDetails, new Dictionary<string, object>
        {
            [Infrastructure.NavigationParams.ItemId] = Item.ItemId.ToString()
        });
    }
}
