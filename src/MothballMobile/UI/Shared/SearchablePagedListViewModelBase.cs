using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging.Abstractions;

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
    private bool disposed;

    [ObservableProperty]
    private string query = string.Empty;

    protected SearchablePagedListViewModelBase(
        IBackgroundTaskObserver backgroundTasks,
        IDebouncer? debouncer,
        int pageSize = 10)
        : base(pageSize)
    {
        this.backgroundTasks = backgroundTasks;
        this.debouncer = debouncer ?? new Debouncer(300, NullLogger<Debouncer>.Instance);
    }

    /// <summary>The background-operation label used while a search runs in the background.</summary>
    protected abstract string SearchOperationName { get; }

    /// <summary>Loads one page using the active search query.</summary>
    protected abstract Task<List<TSource>> LoadPageAsync(string? query, int pageNumber, int pageSize);

    /// <inheritdoc />
    protected sealed override Task<List<TSource>> LoadAsync(int pageNumber, int pageSize)
        => LoadPageAsync(activeQuery, pageNumber, pageSize);

    [RelayCommand]
    protected async Task SearchAsync()
    {
        var requestedQuery = string.IsNullOrWhiteSpace(Query) ? null : Query.Trim();
        await RunCommandAsync(async () =>
        {
            activeQuery = requestedQuery;
            await ReplaceWithFirstPagedAsync();
        });
    }

    partial void OnQueryChanged(string value)
    {
        debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync))
            .FireAndForget(backgroundTasks, SearchOperationName);
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

        disposed = true;
    }
}
