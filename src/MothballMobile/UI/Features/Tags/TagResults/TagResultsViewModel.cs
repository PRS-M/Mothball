using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Application.Features.Containers.Queries;
using CoreApp.Application.Features.Items.Queries;
using CoreApp.Application.Specifications;
using MothballMobile.Infrastructure.Utilities;

namespace MothballMobile.UI.Features.Tags.TagResults;

/// <summary>
/// Loads items and containers assigned to one exact tag.
/// </summary>
public partial class TagResultsViewModel : BaseViewModel, IQueryAttributable, IInitializable, IDisposable
{
    private readonly IItemsListQueryHandler itemQueries;
    private readonly IContainerListQueryHandler containerQueries;
    private readonly IImagePathResolver imagePaths;
    private readonly INavigationService navigation;
    private CancellationTokenSource? loadCancellation;
    private bool initialized;
    private int requestVersion;
    private Guid tagId;
    private string tagName = string.Empty;

    public TagResultsViewModel(
        IItemsListQueryHandler itemQueries,
        IContainerListQueryHandler containerQueries,
        IImagePathResolver imagePaths,
        INavigationService navigation)
    {
        this.itemQueries = itemQueries ?? throw new ArgumentNullException(nameof(itemQueries));
        this.containerQueries = containerQueries ?? throw new ArgumentNullException(nameof(containerQueries));
        this.imagePaths = imagePaths ?? throw new ArgumentNullException(nameof(imagePaths));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public ObservableCollection<TagResultViewModel> Results { get; } = [];

    [ObservableProperty]
    private TagTargetFilter selectedFilter = TagTargetFilter.All;

    [ObservableProperty]
    private string query = string.Empty;

    public IReadOnlyList<TagTargetFilter> AvailableFilters { get; } = Enum.GetValues<TagTargetFilter>();

    public string Title => $"#{tagName}";
    public string SelectedTag => $"#{tagName}";
    public bool HasTag => tagId != Guid.Empty;

    public void ApplyQueryAttributes(IDictionary<string, object> queryParameters)
    {
        if (queryParameters.TryGetValue(NavigationParams.TagId, out var idValue)
            && idValue is string id
            && Guid.TryParse(id, out var parsedId))
        {
            tagId = parsedId;
        }

        if (queryParameters.TryGetValue(NavigationParams.TagName, out var nameValue)
            && nameValue is string name
            && !string.IsNullOrWhiteSpace(name))
        {
            tagName = name.Trim().TrimStart('#');
        }

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(HasTag));
        initialized = false;
    }

    public Task InitializeAsync()
        => initialized ? Task.CompletedTask : ReloadAsync();

    partial void OnSelectedFilterChanged(TagTargetFilter value)
        => ReloadInBackground();

    partial void OnQueryChanged(string value)
        => ReloadInBackground();

    [RelayCommand]
    private Task RefreshAsync() => ReloadAsync();

    [RelayCommand]
    private Task ClearTagAsync() => navigation.GoToAsync("..");

    private void ReloadInBackground()
    {
        if (!initialized || !HasTag)
        {
            return;
        }

        _ = ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var version = Interlocked.Increment(ref requestVersion);
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref loadCancellation, cancellation);
        previous?.Cancel();
        previous?.Dispose();

        try
        {
            await RunCommandAsync(async () =>
            {
                var itemFilter = new TagFilter(TagTargetType.Item, [tagName]);
                var containerFilter = new TagFilter(TagTargetType.Container, [tagName]);
                var search = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim();
                var results = new List<TagResultViewModel>();

                if (SelectedFilter is TagTargetFilter.All or TagTargetFilter.Items)
                {
                    var items = await itemQueries.QueryAsync(
                        ItemQueryFilter.All, search, null, null, itemFilter);
                    cancellation.Token.ThrowIfCancellationRequested();
                    results.AddRange(items.Select(item => new TagResultViewModel(item, imagePaths, navigation)));
                }

                if (SelectedFilter is TagTargetFilter.All or TagTargetFilter.Containers)
                {
                    var containers = await containerQueries.QueryAsync(
                        false, search, null, null, containerFilter);
                    cancellation.Token.ThrowIfCancellationRequested();
                    results.AddRange(containers.Select(container => new TagResultViewModel(container, imagePaths, navigation)));
                }

                if (version != Volatile.Read(ref requestVersion))
                {
                    return;
                }

                Results.Clear();
                foreach (var result in results
                    .OrderBy(result => result.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(result => result.TargetType))
                {
                    Results.Add(result);
                }
                initialized = true;
            }, showRefreshing: true);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref loadCancellation, null, cancellation), cancellation))
            {
                cancellation.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Interlocked.Increment(ref requestVersion);
        var cancellation = Interlocked.Exchange(ref loadCancellation, null);
        cancellation?.Cancel();
        cancellation?.Dispose();
    }
}
