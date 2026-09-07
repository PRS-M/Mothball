using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Domain.Entities.ContainerAggregate;
using CoreApp.Domain.Entities.InventoryAggregate;

namespace MothballMobile.UI.Features.Tags.TagResults;

/// <summary>
/// Presents one item or container returned for a selected tag.
/// </summary>
public partial class TagResultViewModel : ObservableObject
{
    private readonly INavigationService navigation;

    public TagResultViewModel(
        InventorySnapshot item,
        IImagePathResolver imagePaths,
        INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(imagePaths);
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        TargetType = TagTargetFilter.Items;
        TargetId = item.Item.ItemId;
        Name = item.Item.Name;
        SecondaryText = item.Item.Description;
        DetailText = LocalizationManager.Current.Format("Total: {0}", item.TotalQuantity);
        ImagePaths = new ObservableCollection<string>(imagePaths.GetItemPhotoPaths(item.Item));
    }

    public TagResultViewModel(
        Container container,
        IImagePathResolver imagePaths,
        INavigationService navigation)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(imagePaths);
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        TargetType = TagTargetFilter.Containers;
        TargetId = container.ContainerId;
        Name = container.Name;
        SecondaryText = container.Notes;
        DetailText = LocalizationManager.Current.Format("Items stored: {0}", container.TotalItemQuantity);
        ImagePaths = new ObservableCollection<string>(imagePaths.GetContainerPhotoPaths(container));
    }

    public TagTargetFilter TargetType { get; }
    public Guid TargetId { get; }
    public string Name { get; }
    public string SecondaryText { get; }
    public string DetailText { get; }
    public ObservableCollection<string> ImagePaths { get; }
    public string TypeText => TargetType == TagTargetFilter.Items
        ? LocalizationManager.Current.Get("Item")
        : LocalizationManager.Current.Get("Container");

    [RelayCommand]
    private Task OpenAsync()
        => TargetType == TagTargetFilter.Items
            ? navigation.GoToAsync(
                NavigationRoutes.ItemDetails,
                new Infrastructure.Navigation.ItemDetailsNavigationRequest(TargetId))
            : navigation.GoToAsync(
                NavigationRoutes.ContainerDetails,
                new Infrastructure.Navigation.ContainerDetailsNavigationRequest(TargetId));
}
