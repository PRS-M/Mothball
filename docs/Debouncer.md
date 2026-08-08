# Debouncer in Mothball

## Purpose

`Debouncer` is a small concurrency utility that delays an action and ensures only the latest request in a burst is executed.

In this codebase it is used for UI search input so typing does not trigger a repository query on every keystroke.

Primary location:
- `src/MothballMobile/Infrastructure/Debouncer.cs`
- Contract: `src/MothballMobile/Infrastructure/IDebouncer.cs`

## API Contract

```csharp
public interface IDebouncer
{
    Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
```

### Meaning of the API

- `action`: The async work to run after the debounce delay.
- `cancellationToken`: Optional external token that can cancel the request.
- Return value: A `Task` representing that specific scheduled request.

Important: In common UI usage, callers intentionally do not await this task and fire-and-forget it.

## How It Works Internally

`Debouncer` keeps a single active `CancellationTokenSource` (`cts`) and replaces it on every call.

Core steps inside `DebounceAsync`:

1. Validate inputs and early-return if disposed.
2. Enter a lock (`sync`) to make state changes thread-safe.
3. Cancel and dispose the previous `cts` (if any).
4. Create a new linked token source from the caller token.
5. Start async delay+execute pipeline (`DebounceCoreAsync`) using the new token source.

Core steps inside `DebounceCoreAsync`:

1. Await `Task.Delay(delayMs, token)`.
2. If canceled, exit.
3. Run `await action(token)`.
4. In `finally`, lock and dispose `localCts` only if it is still the active `cts`.

This `ReferenceEquals(cts, localCts)` guard prevents one request from disposing a newer request's token source.

## Debounce Semantics

The behavior is trailing-edge debounce:

- If calls happen rapidly inside the debounce window, previous pending calls are canceled.
- Only the last call that survives the full delay runs its action.

Example with 300 ms delay:

- t=0 ms: call A scheduled
- t=90 ms: call B arrives, A canceled
- t=170 ms: call C arrives, B canceled
- t=470 ms: C executes

So the action runs once after the input stabilizes.

## Threading Model

`Debouncer` is thread-safe for concurrent calls.

### What provides thread safety

- A private lock object (`sync`) protects shared mutable fields:
  - `cts`
  - `isDisposed`
- All swaps/cancel/dispose of current `cts` happen under the lock.

### What does not run under the lock

- The user action itself (`action`) does not run inside the lock.
- Delay and action execution are asynchronous and occur outside critical sections.

This is correct and avoids blocking other callers.

### SynchronizationContext behavior

- Internal awaits use `ConfigureAwait(false)`.
- Therefore continuation is not forced onto UI thread.
- If action must touch UI state, caller must marshal to main thread.

Current usage in view models does exactly this:

```csharp
debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync)).Forget();
```

That pattern keeps debouncer generic and UI-agnostic.

## Cancellation and Disposal Behavior

### Per-call cancellation

Each new `DebounceAsync` call cancels the previous pending call.

Effects:
- Pending delays are interrupted with `OperationCanceledException`.
- That exception is swallowed when caused by this token.
- Older actions should not execute once canceled.

### External cancellation token

Caller-provided token is linked into the internal token source.

Effects:
- If caller token cancels before delay completes, action never runs.
- If token cancels while action is running, action can observe token and stop if it supports cancellation.

### Disposal

`Dispose()`:

1. Marks instance disposed.
2. Detaches current `cts` under lock.
3. Cancels and disposes detached `cts`.

After disposal:
- New `DebounceAsync` calls return `Task.CompletedTask`.
- Pending delayed request is canceled.

This makes disposal safe for view model lifecycle shutdown.

## Why This Implementation Is Safe

Safety properties:

- No shared-state race on `cts` because all writes are locked.
- No accidental disposal of newer token source due to `ReferenceEquals` check in `finally`.
- Cancellation is expected flow and not treated as error.
- Idempotent `Dispose()` behavior (safe if called multiple times).

## How Other Services Should Use It

### 1. Use one debouncer per independent input stream

Good:
- One debouncer for item search box.
- Another debouncer for container search box.

Avoid:
- A single global debouncer for unrelated operations, which causes cross-cancellation.

### 2. Pick delay based on interaction

Typical ranges:
- 150-250 ms: very responsive text filtering.
- 250-400 ms: server/database-backed search.
- 500+ ms: expensive work where reduced frequency matters more than responsiveness.

### 3. Keep action cancellation-aware

When possible, pass token into downstream calls, for example repository calls that support cancellation.

Current repository APIs in this solution often do not accept tokens yet; if that changes, wire token through.

### 4. Marshal to UI thread only when needed

- UI state updates: use `MainThread.InvokeOnMainThreadAsync(...)`.
- Pure background work: run directly, no UI dispatch required.

### 5. Dispose with owner lifecycle

If a service or view model owns a debouncer, it should dispose it in `Dispose()`/teardown.

This is already done in `ItemsListViewModel` and should be mirrored in any long-lived owner.

## Recommended Usage Patterns

### UI Search (current pattern)

```csharp
partial void OnQueryChanged(string value)
{
    debouncer.DebounceAsync(_ => MainThread.InvokeOnMainThreadAsync(SearchAsync)).Forget();
}
```

Why it works:
- Fast typing collapses into one query execution.
- Search runs on UI thread where observable collections and bound properties are safe to update.

### UI Search (more thorough example)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class ProductsViewModel : ObservableObject, IDisposable
{
    private readonly IDebouncer debouncer = new Debouncer(300);
    private readonly IProductQueryService productQueryService;

    [ObservableProperty]
    private string query = string.Empty;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? error;

    public ObservableCollection<ProductViewModel> Items { get; } = new();

    public ProductsViewModel(IProductQueryService productQueryService)
    {
        this.productQueryService = productQueryService;
    }

    partial void OnQueryChanged(string value)
    {
        // Fire-and-forget is intentional for UI typing events.
        debouncer
            .DebounceAsync(token => MainThread.InvokeOnMainThreadAsync(() => SearchAsync(token)))
            .Forget();
    }

    [RelayCommand]
    private Task RefreshAsync() => SearchAsync(CancellationToken.None);

    private async Task SearchAsync(CancellationToken token)
    {
        try
        {
            IsBusy = true;
            Error = null;

            var trimmed = Query.Trim();
            var results = string.IsNullOrWhiteSpace(trimmed)
                ? await productQueryService.GetAllAsync(token)
                : await productQueryService.SearchAsync(trimmed, token);

            Items.Clear();
            foreach (var product in results)
            {
                Items.Add(new ProductViewModel(product));
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Expected path during rapid typing; no user-facing error needed.
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Dispose() => debouncer.Dispose();
}
```

What this demonstrates:
- Debouncer owned as a field (not recreated per keystroke).
- Debounced callback receives a cancellation token.
- Work is canceled naturally during rapid typing.
- Exceptions are handled so fire-and-forget does not leak failures.
- Disposal cancels any pending delayed callback.

### Background expensive recomputation

```csharp
await debouncer.DebounceAsync(async token =>
{
    var result = await calculator.RebuildIndexAsync(token);
    cache.Update(result);
}, shutdownToken);
```

Why it works:
- Repeated triggers collapse into one expensive recompute.
- External token supports app/service shutdown.

### Background service (file/index invalidation)

```csharp
public sealed class SearchIndexInvalidationService : IDisposable
{
    private readonly IDebouncer rebuildDebouncer;
    private readonly IIndexBuilder builder;
    private readonly ILogger<SearchIndexInvalidationService> logger;

    public SearchIndexInvalidationService(
        IIndexBuilder builder,
        ILogger<SearchIndexInvalidationService> logger)
    {
        this.builder = builder;
        this.logger = logger;
        rebuildDebouncer = new Debouncer(500);
    }

    public Task NotifyDataChangedAsync(CancellationToken shutdownToken)
    {
        // Many change notifications may arrive quickly.
        // Debouncer ensures one rebuild after notifications calm down.
        return rebuildDebouncer.DebounceAsync(async token =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await builder.RebuildAsync(token);
            sw.Stop();

            logger.LogInformation("Index rebuilt in {ElapsedMs} ms", sw.ElapsedMilliseconds);
        }, shutdownToken);
    }

    public void Dispose() => rebuildDebouncer.Dispose();
}
```

When this pattern is useful:
- File watcher events (create/update/delete bursts).
- Domain event storms where immediate repeated work is wasteful.
- Re-index, recompute, refresh cache, or synchronize pipelines.

### Per-key debouncing (independent streams)

Sometimes each key should debounce independently (for example, one stream per container ID).

```csharp
public sealed class PerKeyDebounceRouter<TKey> : IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, Debouncer> debouncers = new();
    private readonly int delayMs;

    public PerKeyDebounceRouter(int delayMs)
    {
        this.delayMs = delayMs;
    }

    public Task DebounceAsync(
        TKey key,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        var debouncer = debouncers.GetOrAdd(key, _ => new Debouncer(delayMs));
        return debouncer.DebounceAsync(action, cancellationToken);
    }

    public void Dispose()
    {
        foreach (var pair in debouncers)
        {
            pair.Value.Dispose();
        }
        debouncers.Clear();
    }
}
```

Why this matters:
- A single global debouncer causes cross-cancellation.
- Per-key routing isolates each logical stream.
- Still keeps each stream trailing-edge debounced.

### Safe fire-and-forget helper usage

When you intentionally do not await `DebounceAsync`, ensure your `Forget()` helper observes/logs exceptions.

```csharp
public static class TaskExtensions
{
    public static void Forget(this Task task, ILogger? logger = null)
    {
        if (task.IsCompletedSuccessfully)
        {
            return;
        }

        _ = ObserveAsync(task, logger);
    }

    private static async Task ObserveAsync(Task task, ILogger? logger)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Fire-and-forget task failed.");
        }
    }
}
```

Without an exception-observing helper, failures in debounced callbacks can become difficult to diagnose.

## Common Mistakes to Avoid

1. Recreating debouncer on every event callback.
- This defeats debouncing. Keep it as a field.

2. Sharing one debouncer for unrelated workflows.
- Unrelated calls cancel each other unexpectedly.

3. Assuming action always runs.
- It may be canceled by a newer call or disposal.

4. Ignoring UI thread affinity.
- If action mutates UI-bound state from background thread, threading bugs can occur.

5. Forgetting lifecycle disposal.
- Can leave delayed callbacks active longer than owner lifetime.

## Testing Strategy

Existing tests validate two core guarantees:

- Only one execution after rapid repeated calls.
- Disposal suppresses future execution.

Current tests:
- `tests/UnitTests/DebouncerTests.cs`

Additional tests worth adding:

1. External token cancellation prevents execution.
2. Action receives canceled token when canceled mid-flight.
3. Concurrent callers from multiple threads still execute only latest surviving request.
4. Dispose during pending delay cancels execution deterministically.

## Practical Summary

- `Debouncer` is a trailing-edge, thread-safe, cancellation-driven utility.
- It is ideal for bursty triggers like text input or repeated invalidation events.
- Threading correctness comes from lock-protected token source swapping and cancellation.
- UI callers should explicitly dispatch UI work to main thread.
- Owner should dispose the debouncer to cleanly cancel pending callbacks.
