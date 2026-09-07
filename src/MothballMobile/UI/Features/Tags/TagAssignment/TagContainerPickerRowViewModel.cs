using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Domain.Entities.ContainerAggregate;

namespace MothballMobile.UI.Features.Tags.TagAssignment;

/// <summary>Represents a container that can receive a tag.</summary>
public partial class TagContainerPickerRowViewModel : ObservableObject
{
    private readonly Func<Guid, Task> select;

    /// <summary>Creates a container row with the action used to assign the selected tag.</summary>
    /// <param name="container">The container displayed by the row.</param>
    /// <param name="imagePaths">Resolves the container's local photo paths.</param>
    /// <param name="select">Assigns the tag when the row is selected.</param>
    public TagContainerPickerRowViewModel(
        Container container,
        IImagePathResolver imagePaths,
        Func<Guid, Task> select)
    {
        ArgumentNullException.ThrowIfNull(container);
        ArgumentNullException.ThrowIfNull(imagePaths);
        this.select = select ?? throw new ArgumentNullException(nameof(select));
        ContainerId = container.ContainerId;
        Name = container.Name;
        Notes = container.Notes;
        ItemCount = container.TotalItemQuantity.ToString();
        ItemTypesCount = container.ItemTypeCount.ToString();
        ImagePaths = new(imagePaths.GetContainerPhotoPaths(container));
    }

    public Guid ContainerId { get; }
    public string Name { get; }
    public string Notes { get; }
    public string ItemCount { get; }
    public string ItemTypesCount { get; }
    public bool ShowItemCount => true;
    public bool ShowItemTypesCount => false;
    public ObservableCollection<string> ImagePaths { get; }

    [RelayCommand]
    private Task SelectAsync() => select(ContainerId);
}
