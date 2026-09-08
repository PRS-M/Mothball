# Concurrency and Threading

This document records the asynchronous, threading, cancellation, and synchronization model of Mothball. It is an implementation map and review guide, not a promise that every operation runs on a dedicated background thread.

## Executive summary

Mothball is primarily an asynchronous, event-driven MAUI application. Most `async` methods perform asynchronous I/O and resume on whichever context their caller provides. The application does not use a general worker-thread abstraction or a global application lock.

The main coordination mechanisms are:

- MAUI `MainThread`/`Dispatcher` calls for UI-bound state and controls.
- `SemaphoreSlim` for one-time database initialization, JSON-store writes, backup-key access, barcode-scan sessions, and one tag-results reload at a time.
- `lock` for the photo-operation tracker and search debouncer’s small in-memory state.
- `Interlocked` request versions and cancellation-token replacement for stale-search suppression.
- `TaskCompletionSource` for event-to-task bridges such as startup, ads, modal number picking, and barcode scanning.
- A tracked fire-and-forget extension that logs background task failures.

The most important risks found by static inspection are:

1. Startup awaits signing-key access, persistence recovery/seeding, Shell loading, and an optional ad without a cancellation token for the first three stages. The ad and Shell have timeouts, but persistence and a first-time Debug seed do not. Completed Debug seed data now has a versioned preference marker and a lightweight integrity check, so the long seed path is not repeated when the data is intact.
2. `BarcodeScanSession` waits for a result until `CompleteAsync` is called. If the scanner page disappears without completing the session, the original caller can remain blocked and the session gate can remain held.
3. `TagResultsViewModel` starts reloads with `_ = ReloadAsync()`. Its internal error handling updates the view model and rethrows, so an unexpected failure from a property-change or navigation-attribute reload can become an unobserved task exception.
4. The SQLite barcode registry uses a read/check followed by a separate insert or update. The uniqueness index protects the final data, but two concurrent callers can still race and receive a database exception rather than the application’s intended ownership exception. Assignment of an entity and registry update are also separate operations.
5. The Debug seeder is a long sequence of individual asynchronous writes. It is not a single transaction and the startup orchestrator has no single-flight guard; concurrent startup calls could overlap.

These findings do not prove the cause of a freeze. They identify the highest-value areas for runtime logging and targeted tests.

## Execution model

### Application and persistence layers

Application services and persistence adapters expose `Task`-returning methods. SQLite-net and file APIs perform asynchronous I/O. Most infrastructure methods use `ConfigureAwait(false)`, which deliberately avoids requiring the MAUI synchronization context after the await. Examples include:

- `src/Infrastructure/Services/Repositories/*`
- `src/Infrastructure/Services/JsonStore/*`
- `src/Infrastructure/Services/BarcodeRegistry/*`
- `src/CoreApp.Application/Features/Backup/*`
- `src/Infrastructure.Platform.Maui/Services/MobileFileHandler.cs`

`ConfigureAwait(false)` does not create a new thread. It only changes continuation scheduling. CPU-heavy work is explicitly offloaded in two places:

- `CameraHandler.DecodePhotoAsync` uses `Task.Run` for image decoding.
- `SkiaImageMetadataReader.ReadAsync` uses `Task.Run` for image metadata inspection.

The barcode PDF generator performs rendering work synchronously inside its asynchronous workflow; it is not automatically moved to a worker thread.

### UI layer

MAUI event handlers and view-model commands generally begin on the UI thread. UI collections, bindable properties, navigation, popups, and controls must therefore be updated on the UI thread. The code uses:

- `MainThread.InvokeOnMainThreadAsync` when an operation must execute on the UI thread and be awaited.
- `MainThread.BeginInvokeOnMainThread` for queued UI publication.
- `Dispatcher.Dispatch` in `SegmentedSwitch` for a layout refresh.

Examples include `MauiPopupService`, `InventoryBackupWorkflowService`, `BackupSigningKeyTransferService`, `BarcodeScannerPage`, `PhotoDetailsViewModelBase`, list searches, tag suggestions, and `PhotoBackgroundOperationTracker`.

## Synchronization inventory

| Mechanism | Location | Purpose | Assessment |
| --- | --- | --- | --- |
| `SemaphoreSlim initLock` | `Infrastructure/Services/Database/MothballDatabase.cs` | Ensures SQLite schema/connection initialization runs once | Good double-check inside the gate; no cancellation; disposal assumes app-lifetime shutdown rather than concurrent use |
| `SemaphoreSlim writeLock` | `Infrastructure/Services/JsonStore/JsonInventoryStore.cs` | Serializes JSON snapshot writes, recovery, and rollback | Strong per-store-instance write serialization; reads use immutable two-slot snapshots but are not held by the write gate |
| `SemaphoreSlim synchronizationLock` | `MothballMobile/Infrastructure/Backup/BackupSignatureSecretProvider.cs` | Prevents duplicate secure/keychain secret creation or replacement | Good in-process single-flight protection; secure-storage calls have partial cancellation support |
| `SemaphoreSlim sessionGate` | `MothballMobile/Infrastructure/Scanning/BarcodeScanSession.cs` | Allows one active barcode scan | Correctly prevents overlapping scans; missing scanner completion can hold it indefinitely |
| `SemaphoreSlim reloadGate` | `MothballMobile/UI/Features/Tags/TagResults/TagResultsViewModel.cs` | Serializes tag-result reload execution | Good stale-request design with cancellation and version checks; entry points do not always observe the returned task |
| `SemaphoreSlim initializationGate` | `MothballMobile/UI/Shared/BasePage.cs` | Prevents overlapping page initialization calls | Serializes repeated `OnAppearing`; no cancellation on disappearance and the gate is not disposed |
| `lock (sync)` | `MothballMobile/Infrastructure/Resilience/Debouncer.cs` | Atomically replaces the current debounce CTS and handles disposal | Small critical sections; action runs outside the lock |
| `lock (gate)` | `MothballMobile/Infrastructure/BackgroundOperations/Photos/PhotoBackgroundOperationTracker.cs` | Protects active-operation dictionary and banner CTS | State protection is localized; UI publication is queued while the lock is held |
| `Interlocked` request versions | Searchable lists, item/container details, tag results, scanner | Rejects stale async results and accepts one scan result | Appropriate lock-free coordination for small scalar state |
| `TaskCompletionSource` with `RunContinuationsAsynchronously` | Startup coordinator, ad wait, barcode scan, number picker | Converts framework events/modal completion to awaitable tasks | Avoids inline continuation reentrancy; completion ownership must still be guaranteed |

There are no uses of `Monitor`, `Mutex`, `Channel<T>`, `ConcurrentDictionary`, `Parallel.*`, or `Thread.Sleep` in production code. There are no synchronous `.Wait()`, `.Result`, or `GetAwaiter().GetResult()` calls in production code.

## Fire-and-forget work

`MothballMobile/Infrastructure/Utilities/TaskExtensions.cs` defines `FireAndForget`. It observes a task with `ConfigureAwait(false)` and sends failures to `IBackgroundTaskObserver`, implemented by `LoggingBackgroundTaskObserver`.

It is used for:

- Debounced searches and tag suggestions in `SearchablePagedListViewModelBase`.
- Container and item list searches.
- Container-detail searches, suggestions, image loading, and photo persistence.
- Association searches and image loading.
- Add-existing-item image loading.

This is preferable to a completely unobserved discard because failures are logged. It still means the caller does not await completion, cannot directly cancel the operation, and usually cannot prevent the operation from finishing after its page has disappeared. The operation itself must check disposal, cancellation, or request-version state where appropriate.

Two related paths are not routed through `FireAndForget`:

- `TagResultsViewModel` uses `_ = ReloadAsync()` for filter, query, and navigation-attribute changes.
- `SegmentedSwitch` uses `_ = AnimateSelectionAsync(...)` for selection changes.

These paths should be reviewed if runtime logs show unobserved exceptions or UI updates after a page has been replaced.

## Cancellation and stale-result handling

Cancellation is concentrated in interactive search and tag suggestion flows:

- `Debouncer` cancels the previous delay/action when a newer query arrives and cancels its active CTS on disposal.
- `SearchablePagedListViewModelBase` and item/container detail view models replace suggestion CTS instances with `Interlocked.Exchange`, cancel the previous request, and use a version counter before publishing results.
- `TagResultsViewModel` cancels the previous reload, waits on `reloadGate` with the new token, and checks the version before replacing `Results`.
- `PhotoBackgroundOperationTracker` uses cancellation only for its three-second banner-hide timer.

Most repository and backup APIs accept cancellation tokens, but many UI initialization, mutation, image, navigation, and seeding paths do not pass one. Cancellation is therefore cooperative and incomplete rather than a global shutdown mechanism.

Disposal generally cancels current work but does not await its completion. This is acceptable for short-lived suggestion requests only if every continuation checks its version/disposed state before touching UI. It is less safe for long-running image writes and page-level initialization.

## Event handlers and `async void`

`async void` is used where MAUI requires an event or lifecycle handler:

- `BasePage.OnAppearing` and its view-model error event handler.
- `AppShell` barcode-scan event.
- `MainPage` navigation button events.
- Barcode scanner camera/gallery events.
- Number-picker modal events.

The scanner, Shell, and MainPage handlers catch their operation failures. `BasePage.OnAppearing` catches initialization failures and shows an error popup. `BasePage.OnViewModelErrorOccurred` is an `async void` event handler, so failures from popup display cannot be propagated to its raiser. This is a framework-boundary trade-off; event handlers should remain thin and delegate to testable `Task` methods.

## Startup and splash-screen concurrency

`AppStartupCoordinator.InitializeAsync` performs this sequence:

```text
secret provider -> persistence initializer -> Debug seeding -> assign AppShell -> wait for Loaded (5 s) -> optional app-open ad (5 s)
```

The Shell-loaded and ad waits use `TaskCompletionSource` with asynchronous continuations and bounded waits. A Shell `Loaded` timeout prevents a missed event from leaving the splash page permanently visible.

The following stages have no timeout or caller cancellation:

- `BackupSignatureSecretProvider.GetOrCreateAsync` secure-storage/keychain access.
- `AppStartupOrchestrator.StartAsync` persistence initialization and recovery.
- Debug demo seeding, including 100 containers, 100 items per container, photos, barcodes, tags, and assignments.

The Debug seed is intentionally large and performs many sequential operations. On a fresh device, it can make the splash screen appear frozen even when the process is progressing. Instrumentation already logs signing-key and persistence elapsed time in `AppStartupCoordinator`; the seeder itself does not currently report progress or per-phase timing. After successful completion, the orchestrator stores `DemoDataSeeder.SeedVersion` in preferences and checks seed-marker/container/item counts before skipping the expensive path on subsequent startups.

There is no startup single-flight gate. The normal window lifecycle invokes initialization once, and the retry page invokes it after a failure, but defensive protection against duplicate calls is not present.

## Persistence concurrency

### SQLite

`MothballDatabase` protects connection/schema initialization with `initLock`. Higher-level multi-row changes use `SqliteTransactionRunner` and a synchronous `SQLiteConnection` transaction body for operations that must commit together, notably item allocation and withdrawal.

Not every multi-step workflow is transactional. For example, `BarcodeAssignmentService` checks inventory ownership, updates the registry, updates the entity, and releases the old registry entry as separate operations. `SqliteBarcodeRegistryService` also performs availability checks and insert/update operations separately. The database uniqueness index is the final protection against duplicate normalized values, but callers can still observe race-dependent exceptions or an intermediate registry/entity mismatch after a later step fails.

### JSON operational store

`JsonInventoryStore` serializes all `UpdateAsync`, recovery, and rollback operations with one `SemaphoreSlim`. Each update reads the active slot, mutates an in-memory state, writes the inactive slot, then writes the inactive manifest. Readers select a complete active slot and do not hold the write gate, so reads can proceed without waiting for a write.

This is safe for concurrent callers sharing the registered store instance. The lock is not inter-process and does not coordinate two independently constructed store instances pointing at the same files. The updater delegate is awaited while the write gate is held; it must remain short and must not call back into another operation that waits for the same store.

## Background photo operations

`PhotoBackgroundOperationTracker` is an in-memory state machine. Start/report/complete calls are protected by `lock (gate)`. Property and collection notifications are dispatched to the MAUI main thread. When the last operation completes, a detached three-second `Task.Run` hides the banner unless a newer operation cancels that timer.

The tracker does not own or await the actual photo persistence task. Callers start the tracker, run persistence through a tracked fire-and-forget operation, and complete the tracker from the persistence workflow. A missing completion call can leave the banner and active-operation count inconsistent. The banner timer catches cancellation, but unexpected exceptions in the detached task would not be delivered to `IBackgroundTaskObserver`.

## Review priorities

If the app freezes after inactivity or during startup, investigate in this order:

1. Add phase-duration and operation-count logs around Debug seeding, including photo copies and barcode/tag assignments.
2. Add a startup cancellation/single-flight policy and a bounded timeout or progress surface for persistence and seeding.
3. Ensure scanner disappearance completes the pending `BarcodeScanSession` with `null` and releases the gate.
4. Route all detached reloads and animation tasks through a common observer, or make their `Task` lifetimes explicit.
5. Add an application-level transaction/serialization boundary for barcode registry plus entity updates.
6. Add lifecycle cancellation tokens to page initialization, image persistence, and background operations, and await or explicitly detach them during disposal.

## Audit method and limitations

This audit used a repository-wide static search for `async`, `await`, `Task.Run`, `TaskCompletionSource`, `SemaphoreSlim`, `lock`, `Interlocked`, cancellation tokens, UI dispatch, timers, fire-and-forget helpers, blocking waits, and parallel APIs. It reviewed production implementations and relevant tests, but did not include runtime profiling, memory dumps, platform scheduler traces, or device-specific reproduction. The findings should therefore guide instrumentation and targeted tests rather than be treated as a definitive root-cause diagnosis.
