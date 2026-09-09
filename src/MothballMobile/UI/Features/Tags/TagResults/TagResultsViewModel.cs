using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Application.Features.Containers.Queries;
using CoreApp.Application.Features.Items.Queries;
using CoreApp.Application.Specifications;
using MothballMobile.Infrastructure.BackgroundOperations.Observability;
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
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly SemaphoreSlim reloadGate = new(1, 1);
    private CancellationTokenSource? loadCancellation;
    private bool initialized;
    private bool initializationAttempted;
    private int requestVersion;
    private Guid tagId;
    private string tagName = string.Empty;

    public TagResultsViewModel(
        IItemsListQueryHandler itemQueries,
        IContainerListQueryHandler containerQueries,
        IImagePathResolver imagePaths,
        INavigationService navigation,
        IBackgroundTaskObserver backgroundTasks)
    {
        this.itemQueries = itemQueries ?? throw new ArgumentNullException(nameof(itemQueries));
        this.containerQueries = containerQueries ?? throw new ArgumentNullException(nameof(containerQueries));
        this.imagePaths = imagePaths ?? throw new ArgumentNullException(nameof(imagePaths));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.backgroundTasks = backgroundTasks ?? throw new ArgumentNullException(nameof(backgroundTasks));
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
        var wasInitialized = initialized;

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

        var activeLoad = Volatile.Read(ref loadCancellation);
        if (activeLoad is not null)
        {
            Interlocked.Increment(ref requestVersion);
        }

        if ((wasInitialized || initializationAttempted || activeLoad is not null) && HasTag)
        {
            ReloadAsync().FireAndForget(backgroundTasks, "Reload tag results after navigation");
        }
    }

    public Task InitializeAsync()
    {
        initializationAttempted = true;
        return !HasTag ? Task.CompletedTask : ReloadAsync();
    }

    partial void OnSelectedFilterChanged(TagTargetFilter value)
        => ReloadInBackground();

    partial void OnQueryChanged(string value)
        => ReloadInBackground();

    [RelayCommand]
    private Task RefreshAsync()
        => IsBusy ? Task.CompletedTask : ReloadAsync();

    /// <summary>Shows existing items and assigns the selected tag to the chosen item.</summary>
    [RelayCommand]
    private Task AddItemAsync()
        => navigation.GoToAsync(
            NavigationRoutes.TagItemPicker,
            new Dictionary<string, object>
            {
                [NavigationParams.TagId] = tagId.ToString(),
                [NavigationParams.TagName] = tagName,
            });

    /// <summary>Shows existing containers and assigns the selected tag to the chosen container.</summary>
    [RelayCommand]
    private Task AddContainerAsync()
        => navigation.GoToAsync(
            NavigationRoutes.TagContainerPicker,
            new Dictionary<string, object>
            {
                [NavigationParams.TagId] = tagId.ToString(),
                [NavigationParams.TagName] = tagName,
            });

    private void ReloadInBackground()
    {
        if (!initialized || !HasTag)
        {
            return;
        }

        ReloadAsync().FireAndForget(backgroundTasks, "Reload tag results after filter change");
    }

    private async Task ReloadAsync()
    {
        var version = Interlocked.Increment(ref requestVersion);
        var cancellation = new CancellationTokenSource();
        var previous = Interlocked.Exchange(ref loadCancellation, cancellation);
        previous?.Cancel();
        var gateAcquired = false;

        try
        {
            await reloadGate.WaitAsync(cancellation.Token);
            gateAcquired = true;
            await RunCommandAsync(async () =>
            {
                var itemFilter = new TagFilter(TagTargetType.Item, [tagName], TagId: tagId);
                var containerFilter = new TagFilter(TagTargetType.Container, [tagName], TagId: tagId);
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
            if (gateAcquired)
            {
                reloadGate.Release();
            }

            Interlocked.CompareExchange(ref loadCancellation, null, cancellation);
            cancellation.Dispose();
        }
    }

    public void Dispose()
    {
        Interlocked.Increment(ref requestVersion);
        var cancellation = Interlocked.Exchange(ref loadCancellation, null);
        cancellation?.Cancel();
    }
}
