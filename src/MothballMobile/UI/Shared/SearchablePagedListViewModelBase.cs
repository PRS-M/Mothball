using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;
using CoreApp.Application.Abstractions.Persistence;
using CoreApp.Application.Contracts.Tags;

namespace MothballMobile.UI.Shared;

/// <summary>
/// Adds debounced, query-driven search and a refresh command to a paged list view model.
/// </summary>
public abstract partial class SearchablePagedListViewModelBase<TSource, TViewModel>
    : PagedListViewModelBase<TSource, TViewModel>, IDisposable
{
    protected readonly IBackgroundTaskObserver backgroundTasks;
    private readonly IDebouncer debouncer;
    private string? activeQuery;
    private int searchRequestVersion;
    private bool disposed;
    private readonly ITagRepository? tagRepository;
    private CancellationTokenSource? tagSuggestionCancellation;

    [ObservableProperty]
    private string query = string.Empty;

    /// <summary>Gets the exact tags currently applied to the list query.</summary>
    public ObservableCollection<TagDescriptor> SelectedTags { get; } = [];

    /// <summary>Gets the tag suggestions matching the active hash token.</summary>
    public ObservableCollection<TagDescriptor> SuggestedTags { get; } = [];

    /// <summary>Gets a value indicating whether at least one tag filter is active.</summary>
    public bool HasSelectedTags => SelectedTags.Count > 0;

    /// <summary>Gets a value indicating whether the suggestion surface should be shown.</summary>
    public bool IsTagSuggestionsVisible => SuggestedTags.Count > 0;

    protected SearchablePagedListViewModelBase(
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer,
        int pageSize = 10,
        IPagedListLoadDiagnostics? loadDiagnostics = null,
        ITagRepository? tagRepository = null)
        : base(pageSize, loadDiagnostics)
    {
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(300, NullLogger<Debouncer>.Instance);
        this.tagRepository = tagRepository;
    }

    /// <summary>The background-operation label used while a search runs in the background.</summary>
    protected abstract string SearchOperationName { get; }
    protected bool HasActiveQuery => activeQuery is not null;
    protected override string LoadVariant => HasActiveQuery ? "search" : "browse";

    /// <summary>Loads one page using the active search query.</summary>
    protected abstract Task<List<TSource>> LoadPageAsync(string? query, int pageNumber, int pageSize);

    /// <summary>Identifies the aggregate type used when selected tags are applied.</summary>
    protected abstract TagTargetType TagTargetType { get; }

    /// <summary>Gets the exact tag criteria currently selected in the search surface.</summary>
    protected TagFilter? CurrentTagFilter => SelectedTags.Count == 0
        ? null
        : new TagFilter(TagTargetType, SelectedTags.Select(tag => tag.Name).ToArray());

    /// <inheritdoc />
    protected sealed override Task<List<TSource>> LoadAsync(int pageNumber, int pageSize)
        => LoadPageAsync(activeQuery, pageNumber, pageSize);

    [RelayCommand]
    protected async Task SearchAsync()
    {
        var requestVersion = Interlocked.Increment(ref searchRequestVersion);
        var requestedQuery = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim();
        if (IsBusy)
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            activeQuery = requestedQuery;
            await ReplaceWithFirstPagedAsync();
        });

        if (requestVersion != Volatile.Read(ref searchRequestVersion))
        {
            await SearchAsync();
        }
    }

    partial void OnQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync))
            .FireAndForget(backgroundTasks, SearchOperationName);

        RefreshTagSuggestionsAsync(value)
            .FireAndForget(backgroundTasks, $"{SearchOperationName} tag suggestions");
    }

    /// <summary>Adds a suggestion as an exact tag filter and refreshes the first page.</summary>
    /// <param name="tag">The tag selected by the user.</param>
    [RelayCommand]
    public void AddTag(TagDescriptor? tag)
    {
        if (tag is null || SelectedTags.Any(selected => selected.TagId == tag.TagId))
        {
            return;
        }

        SelectedTags.Add(tag);
        OnPropertyChanged(nameof(HasSelectedTags));
        RemoveTagTokenFromQuery(tag.Name);
        SuggestedTags.Clear();
        OnPropertyChanged(nameof(IsTagSuggestionsVisible));
        MainThread.InvokeOnMainThreadAsync(SearchAsync)
            .FireAndForget(backgroundTasks, SearchOperationName);
    }

    /// <summary>Removes an active tag filter and refreshes the first page.</summary>
    /// <param name="tag">The active tag to remove.</param>
    [RelayCommand]
    public void RemoveTag(TagDescriptor? tag)
    {
        if (tag is null || !SelectedTags.Remove(tag))
        {
            return;
        }

        OnPropertyChanged(nameof(HasSelectedTags));
        MainThread.InvokeOnMainThreadAsync(SearchAsync)
            .FireAndForget(backgroundTasks, SearchOperationName);
    }

    private async Task RefreshTagSuggestionsAsync(string value)
    {
        var cancellation = new CancellationTokenSource();
        var previousCancellation = Interlocked.Exchange(ref tagSuggestionCancellation, cancellation);
        previousCancellation?.Cancel();
        previousCancellation?.Dispose();
        try
        {
            if (tagRepository is null)
            {
                return;
            }

            var token = ExtractTagToken(value);
            if (token is null)
            {
                SuggestedTags.Clear();
                OnPropertyChanged(nameof(IsTagSuggestionsVisible));

                return;
            }

            var selectedIds = SelectedTags.Select(tag => tag.TagId).ToHashSet();
            var tags = await tagRepository.GetAllAsync(cancellation.Token).ConfigureAwait(false);
            cancellation.Token.ThrowIfCancellationRequested();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            SuggestedTags.Clear();
            foreach (var tag in tags
                .Where(tag => !selectedIds.Contains(tag.TagId)
                    && tag.Name.Value.StartsWith(token, StringComparison.OrdinalIgnoreCase))
                .Take(5))
            {
                SuggestedTags.Add(new TagDescriptor(tag.TagId, tag.Name.Value));
            }

            OnPropertyChanged(nameof(IsTagSuggestionsVisible));
        });
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer query owns the suggestion surface.
        }
        finally
        {
            if (ReferenceEquals(Interlocked.CompareExchange(ref tagSuggestionCancellation, null, cancellation), cancellation))
            {
                cancellation.Dispose();
            }
        }
    }

    private static string? ExtractTagToken(string value)
    {
        var tokenStart = value.LastIndexOf('#');
        if (tokenStart < 0)
        {
            var plainToken = value.Trim();
            return plainToken.Length > 0 && !plainToken.Any(char.IsWhiteSpace) ? plainToken : null;
        }

        if (tokenStart > 0 && !char.IsWhiteSpace(value[tokenStart - 1]))
        {
            return null;
        }

        var token = value[(tokenStart + 1)..];
        return token.Any(char.IsWhiteSpace) ? null : token;
    }

    private void RemoveTagTokenFromQuery(string tagName)
    {
        var tokenStart = Query.LastIndexOf('#');
        if (tokenStart < 0)
        {
            if (string.Equals(Query.Trim(), tagName, StringComparison.OrdinalIgnoreCase))
            {
                Query = string.Empty;
            }

            return;
        }

        Query = Query.Remove(tokenStart).TrimEnd();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing && debouncer is IDisposable d)
        {
            d.Dispose();
        }

        if (disposing)
        {
            tagSuggestionCancellation?.Cancel();
            tagSuggestionCancellation?.Dispose();
            tagSuggestionCancellation = null;
        }

        disposed = true;
    }
}
