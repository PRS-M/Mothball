using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Features.Items.Queries;
using CoreApp.Application.Specifications;
using CoreApp.Domain.Entities.InventoryAggregate;
using MothballMobile.Infrastructure;
using MothballMobile.Infrastructure.Navigation;
using MothballMobile.Infrastructure.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MothballMobile.UI.Features.Tags.TagAssignment;

/// <summary>Provides a paged item picker for assigning a tag.</summary>
public partial class TagItemPickerViewModel : PagedListViewModelBase<InventorySnapshot, TagItemPickerRowViewModel>, IQueryAttributable
{
    private readonly IItemsListQueryHandler itemQueries;
    private readonly IImagePathResolver imagePaths;
    private readonly ITagRepository tags;
    private readonly INavigationService navigation;
    private Guid tagId;
    private string tagName = string.Empty;
    private string? activeQuery;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    /// <summary>Creates a paged picker for existing items.</summary>
    /// <param name="itemQueries">Queries item inventory pages.</param>
    /// <param name="imagePaths">Resolves item photo paths.</param>
    /// <param name="tags">Persists tag assignments.</param>
    /// <param name="navigation">Returns to the tag results after assignment.</param>
    public TagItemPickerViewModel(
        IItemsListQueryHandler itemQueries,
        IImagePathResolver imagePaths,
        ITagRepository tags,
        INavigationService navigation)
    {
        this.itemQueries = itemQueries ?? throw new ArgumentNullException(nameof(itemQueries));
        this.imagePaths = imagePaths ?? throw new ArgumentNullException(nameof(imagePaths));
        this.tags = tags ?? throw new ArgumentNullException(nameof(tags));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    /// <summary>Gets the tag name shown in the page header.</summary>
    public string Title => $"#{tagName}";

    /// <summary>Receives the tag identity passed by the tag-results page.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(NavigationParams.TagId, out var idValue)
            && idValue is string id
            && Guid.TryParse(id, out var parsedId))
        {
            tagId = parsedId;
        }

        if (query.TryGetValue(NavigationParams.TagName, out var nameValue)
            && nameValue is string name)
        {
            tagName = name.Trim().TrimStart('#');
            OnPropertyChanged(nameof(Title));
        }
    }

    protected override Task<List<InventorySnapshot>> LoadAsync(int pageNumber, int pageSize)
        => itemQueries.QueryAsync(ItemQueryFilter.All, activeQuery, pageNumber, pageSize);

    protected override TagItemPickerRowViewModel MapToViewModel(InventorySnapshot source)
        => new(source, imagePaths, AssignAsync);

    [RelayCommand]
    private Task ApplySearchAsync()
        => RunCommandAsync(async () =>
        {
            activeQuery = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim();
            await ReplaceWithFirstPagedAsync();
        }, showRefreshing: true);

    private async Task AssignAsync(Guid itemId)
    {
        await RunCommandAsync(async () =>
        {
            await tags.AssignAsync(tagId, CoreApp.Application.Contracts.Tags.TagTargetType.Item, itemId);
            await navigation.GoBackAsync();
        });
    }
}
