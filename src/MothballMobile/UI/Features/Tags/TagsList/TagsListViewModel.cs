using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Abstractions.DomainEvents;
using CoreApp.Application.Contracts.Tags;
using CoreApp.Domain.Abstractions;
using CoreApp.Domain.Events;
using MothballMobile.Infrastructure.BackgroundOperations.Observability;
using MothballMobile.Infrastructure.Utilities;

namespace MothballMobile.UI.Features.Tags.TagsList;

/// <summary>
/// Loads and filters the catalogue of reusable tags.
/// </summary>
public partial class TagsListViewModel : BaseViewModel, IInitializable, IDisposable
{
    private const int PageSize = 20;
    private readonly ITagRepository tagRepository;
    private readonly INavigationService navigation;
    private readonly IBackgroundTaskObserver backgroundTasks;
    private readonly IDisposable? domainEventSubscription;
    private string? activeQuery;
    private int currentPage;
    private bool hasMorePages = true;

    public TagsListViewModel(
        ITagRepository tagRepository,
        INavigationService navigation,
        IBackgroundTaskObserver backgroundTasks,
        IDomainEventStream? domainEventStream = null)
    {
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        this.backgroundTasks = backgroundTasks ?? throw new ArgumentNullException(nameof(backgroundTasks));
        domainEventSubscription = domainEventStream?.Subscribe(OnDomainEvent);
    }

    public ObservableCollection<TagViewModel> Tags { get; } = [];

    [ObservableProperty]
    private string query = string.Empty;

    [ObservableProperty]
    private string newTagName = string.Empty;

    [ObservableProperty]
    private bool isAddTagFormVisible;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
        // Usage counts can change while a tag-details page is on the navigation stack.
        // Refresh whenever this page appears so returning from an assignment shows current counts.
        => RunCommandAsync(() => RefreshCoreAsync(cancellationToken), showRefreshing: true);

    [RelayCommand]
    private Task RefreshAsync()
        => RunCommandAsync(() => RefreshCoreAsync(), showRefreshing: true);

    [RelayCommand]
    private Task LoadNextPageAsync()
        => IsBusy || !hasMorePages
            ? Task.CompletedTask
            : RunCommandAsync(() => LoadNextPageCoreAsync());

    private async Task LoadNextPageCoreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var page = await tagRepository.GetUsageSummariesPageAsync(activeQuery, currentPage, PageSize);
        cancellationToken.ThrowIfCancellationRequested();
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

    private async Task RefreshCoreAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        currentPage = 0;
        hasMorePages = true;
        Tags.Clear();
        await LoadNextPageCoreAsync(cancellationToken);
    }

    partial void OnQueryChanged(string value)
    {
        activeQuery = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        RefreshAsync().FireAndForget(backgroundTasks, "Refresh tags");
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
            if (domainEventSubscription is null)
            {
                await RefreshCoreAsync();
            }
        }, rethrowOnError: false);
    }

    public void Dispose()
    {
        domainEventSubscription?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void OnDomainEvent(IDomainEvent domainEvent)
    {
        if (domainEvent is not (TagCreated or TagAssigned or TagUnassigned or TagRenamed))
        {
            return;
        }

        MainThread.InvokeOnMainThreadAsync(RefreshAsync)
            .FireAndForget(backgroundTasks, "Refresh tags from domain event");
    }
}
