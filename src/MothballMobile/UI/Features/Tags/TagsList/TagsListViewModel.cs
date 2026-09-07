using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using MothballMobile.Infrastructure.Utilities;

namespace MothballMobile.UI.Features.Tags.TagsList;

/// <summary>
/// Loads and filters the catalogue of reusable tags.
/// </summary>
public partial class TagsListViewModel : BaseViewModel, IInitializable
{
    private readonly ITagRepository tagRepository;
    private readonly INavigationService navigation;
    private IReadOnlyList<TagUsageSummary> allTags = [];

    public TagsListViewModel(ITagRepository tagRepository, INavigationService navigation)
    {
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public ObservableCollection<TagViewModel> Tags { get; } = [];

    [ObservableProperty]
    private string query = string.Empty;

    public Task InitializeAsync()
        // Usage counts can change while a tag-details page is on the navigation stack.
        // Refresh whenever this page appears so returning from an assignment shows current counts.
        => RefreshAsync();

    [RelayCommand]
    private Task RefreshAsync()
        => RunCommandAsync(async () =>
        {
            allTags = await tagRepository.GetUsageSummariesAsync();
            ApplyFilter();
        }, showRefreshing: true);

    partial void OnQueryChanged(string value)
        => ApplyFilter();

    private void ApplyFilter()
    {
        var search = Query.Trim().TrimStart('#');
        var selected = allTags.Where(tag => string.IsNullOrWhiteSpace(search)
            || tag.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        Tags.Clear();
        foreach (var tag in selected)
        {
            Tags.Add(new TagViewModel(tag, navigation));
        }
    }
}
