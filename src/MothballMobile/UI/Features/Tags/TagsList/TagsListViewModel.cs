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
    private const int PageSize = 20;
    private readonly ITagRepository tagRepository;
    private readonly INavigationService navigation;
    private string? activeQuery;
    private int currentPage;
    private bool hasMorePages = true;

    public TagsListViewModel(ITagRepository tagRepository, INavigationService navigation)
    {
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public ObservableCollection<TagViewModel> Tags { get; } = [];

    [ObservableProperty]
    private string query = string.Empty;

    [ObservableProperty]
    private string newTagName = string.Empty;

    [ObservableProperty]
    private bool isAddTagFormVisible;

    public Task InitializeAsync()
        // Usage counts can change while a tag-details page is on the navigation stack.
        // Refresh whenever this page appears so returning from an assignment shows current counts.
        => RefreshAsync();

    [RelayCommand]
    private Task RefreshAsync()
        => RunCommandAsync(RefreshCoreAsync, showRefreshing: true);

    [RelayCommand]
    private Task LoadNextPageAsync()
        => IsBusy || !hasMorePages
            ? Task.CompletedTask
            : RunCommandAsync(LoadNextPageCoreAsync);

    private async Task LoadNextPageCoreAsync()
    {
        var page = await tagRepository.GetUsageSummariesPageAsync(activeQuery, currentPage, PageSize);
        foreach (var tag in page)
        {
            Tags.Add(new TagViewModel(tag, navigation));
        }

        hasMorePages = page.Count == PageSize;
        if (page.Count > 0)
        {
            currentPage++;
        }
    }

    private async Task RefreshCoreAsync()
    {
        currentPage = 0;
        hasMorePages = true;
        Tags.Clear();
        await LoadNextPageCoreAsync();
    }

    partial void OnQueryChanged(string value)
    {
        activeQuery = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        _ = RefreshAsync();
    }

    [RelayCommand]
    private void ShowAddTagForm()
    {
        NewTagName = string.Empty;
        IsAddTagFormVisible = true;
    }

    [RelayCommand]
    private void CancelAddTag()
    {
        NewTagName = string.Empty;
        IsAddTagFormVisible = false;
    }

    [RelayCommand]
    private async Task AddTagAsync()
    {
        var name = NewTagName.Trim().TrimStart('#');
        if (string.IsNullOrWhiteSpace(name))
        {
            await RunCommandAsync(
                () => Task.FromException(new InvalidOperationException("Tag name is required.")),
                errorMessageFactory: _ => LocalizationManager.Current.Get("Tag name is required."),
                rethrowOnError: false);
            return;
        }

        await RunCommandAsync(async () =>
        {
            await tagRepository.GetOrCreateAsync(new CoreApp.Domain.ValueObjects.TagName(name));
            NewTagName = string.Empty;
            IsAddTagFormVisible = false;
            await RefreshCoreAsync();
        }, rethrowOnError: false);
    }
}
