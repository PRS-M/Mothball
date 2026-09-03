# Developer Documentation

This guide is for contributors opening Mothball for the first time. It explains how the solution is organized, where new code belongs, and the usual path for adding a feature.

Mothball is a .NET MAUI app for cataloging containers and items. It has separate Domain and Application layers, interchangeable SQLite and JSON persistence backends, and a MAUI application project for the user interface and device-specific services.

## Start Here

From the repository root, restore and build the solution:

```bash
dotnet build Mothball.sln
```

Run the test suite:

```bash
dotnet test Mothball.Tests.slnf -v minimal
```

To run the app, open the workspace in VS Code or Visual Studio, select `MothballMobile` as the startup project, choose an iOS simulator, Mac Catalyst, or Windows target, and start debugging.

Development prerequisites are listed in the [README](../README.md). On macOS, iOS and Mac Catalyst development also require Xcode and the MAUI workload.

## Solution Map

| Location | Responsibility |
| --- | --- |
| `src/CoreApp.Domain` | Domain entities, aggregate markers, and format-independent inventory policies. It has no project or package dependencies. |
| `src/CoreApp.Application` | Use cases, contracts, ports, specifications, backup workflows, and platform-independent utilities. It references Domain. |
| `src/Infrastructure` | Persistence implementations: SQLite, the JSON operational store, repository adapters, data models, mappings, startup, and restore services. |
| `src/Infrastructure.Platform.Maui` | Implementations that require MAUI or a device platform, such as camera and file access. |
| `src/MothballMobile` | The MAUI app: pages, view models, navigation, app composition, resources, and platform setup. |
| `tests/UnitTests` | NUnit unit tests for core behavior, view models, and utilities. |
| `tests/IntegrationTests` | NUnit integration tests for persistence, restore, and backend parity behavior. |
| `tests/Tests.Shared` | Shared test-only stubs and support code used by multiple test projects. |
| `docs` | User and contributor documentation. |

The project references flow in one direction:

```text
MothballMobile -> CoreApp.Application -> CoreApp.Domain
MothballMobile -> Infrastructure.Persistence -> CoreApp.Application
MothballMobile -> Infrastructure.Platform.Maui -> CoreApp.Application
```

Keep `CoreApp.Domain` independent of Application, Infrastructure, MAUI, persistence, serialization, and device APIs. Keep `CoreApp.Application` independent of MAUI, SQLite, and device APIs. This keeps behavior testable and lets the SQLite and JSON backends implement the same application contracts.

## How the App Is Put Together

A typical user action follows this path:

```text
Page (XAML) -> ViewModel -> Application feature handler/service -> repository contract
    -> SQLite or JSON implementation -> persisted data
```

- Pages and view models present data and collect user input.
- Application feature handlers contain application actions and queries, such as creating an item or loading container details.
- Repository contracts describe the data operations needed by the application.
- Infrastructure provides those operations for the selected backend.
- `MauiProgram.cs` and `Composition/ServiceCollectionExtensions.cs` register everything with dependency injection.

This separation is a guide, not ceremony for its own sake: put behavior in the lowest layer that can own it without depending on UI details.

## Main Folders

### `src/CoreApp.Domain`

Use this project for the business model and policies that must remain independent of external formats and technical concerns.

- `Entities`: aggregate roots, value types, and inventory state.
- `Inventory`: format-independent inventory merge and withdrawal policies.
- `Abstractions`: domain markers such as `IAggregateRoot`.

Do not add repository contracts, DTOs, logging, backup payloads, JSON parsing, persistence behavior, or platform APIs here.

### `src/CoreApp.Application`

Use this project for use cases that should work regardless of UI platform or persistence backend.

- `Abstractions`: repository, platform, and startup ports required by application use cases.
- `Contracts`: data transfer and result types used across layers.
- `Features`: focused application workflows. Backup-related code, for example, is grouped by export, restore, archive, and serialization responsibilities.
- `Specifications`: interfaces for repositories and application services.
- `Utilities`: small reusable, platform-independent helpers.

When a new workflow needs data access, depend on an existing contract where possible. Add a new contract only when the application has a clear new capability to express.

### `src/Infrastructure`

Use this project for data storage and persistence-specific behavior.

- `DatabaseModels` and `Mappers`: SQLite row models and conversions to application models.
- `Services/Repositories`: SQLite-backed repository implementations and query/command adapters.
- `Services/JsonStore`: JSON operational store, repository implementations, recovery, and maintenance support.
- `Services/Restore`: backend-aware backup restore implementations.
- `Services/Startup`: backend startup initialization.
- `Services/Seeding`: Debug-only demo data generation.

The SQLite and JSON backends are both registered through `AddPersistence`. When changing shared persistence behavior, consider whether both backends need an equivalent implementation.

### `src/Infrastructure.Platform.Maui`

Put services here when they require MAUI platform APIs or device capabilities. This keeps platform dependencies out of the core and persistence projects.

### `src/MothballMobile`

Use this project for the app experience and composition.

- `UI/Features`: feature-oriented page and view model folders. Containers, items, settings, and background operations each have their own area.
- `UI/Shared`: reusable page and view model base classes.
- `Infrastructure`: UI-facing adapters for navigation, resilience, settings, popups, startup, and background work.
- `Composition`: dependency injection registration and backend selection.
- `Resources`: images, fonts, styles, raw assets, splash screen, and application icon.
- `AppShell.xaml` and `AppShell.xaml.cs`: top-level navigation and registered routes.

## Add a UI Feature

For a new screen, start in a feature folder under `src/MothballMobile/UI/Features`. Keep the page, code-behind, view model, and small presentation-specific models together.

For example, a new item-history screen might begin like this:

```text
src/MothballMobile/UI/Features/Items/ItemHistory/
  ItemHistoryPage.xaml
  ItemHistoryPage.xaml.cs
  ItemHistoryViewModel.cs
```

The view model should receive its dependencies through the constructor. Use CommunityToolkit MVVM attributes for observable state and commands, following existing view models.

```csharp
public partial class ItemHistoryViewModel : BaseViewModel
{
    private readonly IItemHistoryQueryHandler historyQueries;

    [ObservableProperty]
    private IReadOnlyList<ItemHistoryEntry> entries = [];

    public ItemHistoryViewModel(IItemHistoryQueryHandler historyQueries)
    {
        this.historyQueries = historyQueries;
    }

    [RelayCommand]
    private async Task LoadAsync(Guid itemId)
    {
        Entries = await historyQueries.GetAsync(itemId);
    }
}
```

To make the screen reachable:

1. Register its view model in `AddViewModels` in `Composition/ServiceCollectionExtensions.cs`.
2. Add a route constant in `Infrastructure/Navigation/NavigationRoutes.cs`.
3. Register the page route in `AppShell.xaml.cs`.
4. Navigate through `INavigationService` rather than calling Shell directly from a view model.
5. Add a Shell entry in `AppShell.xaml` only when the screen belongs in top-level navigation.

Pages should use compiled bindings where possible by setting `x:DataType`, and layouts should follow the existing MAUI patterns. Use `CollectionView` for longer lists rather than `ListView`.

## Add Application Behavior

Put a new use case in the appropriate `CoreApp.Application/Features` area. A query or command handler usually makes a good seam between UI and persistence: it accepts the input needed for one user action, uses repository contracts, and returns a result suitable for the caller.

```csharp
public interface IItemHistoryQueryHandler
{
    Task<IReadOnlyList<ItemHistoryEntry>> GetAsync(Guid itemId);
}

public sealed class ItemHistoryQueryHandler : IItemHistoryQueryHandler
{
    private readonly IInventoryQueryRepository inventoryQueries;

    public ItemHistoryQueryHandler(IInventoryQueryRepository inventoryQueries)
    {
        this.inventoryQueries = inventoryQueries;
    }

    public Task<IReadOnlyList<ItemHistoryEntry>> GetAsync(Guid itemId)
    {
        // Query through the application contract and return an application result.
        throw new NotImplementedException();
    }
}
```

Then register the handler in `AddCoreApplication`. Avoid placing business rules in page code-behind or directly in repository implementations when the rule should behave the same for every backend.

## Change Stored Data

Start with the application-facing model and contract in `CoreApp.Application`, then implement the required storage behavior in `Infrastructure`. Move invariant-bearing behavior to `CoreApp.Domain` when it does not depend on external formats or technical concerns.

For a change that affects items, containers, images, or relations:

1. Update the relevant Domain entity or Application contract/repository interface.
2. Update the SQLite data model, mapper, and repository implementation as required.
3. Update the JSON store model and repository implementation with equivalent behavior.
4. Check backup/export and restore behavior when the new data should be portable.
5. Add or update unit tests for both normal behavior and backend parity.

Do not reference MAUI APIs from the persistence project. Conversely, do not make the UI depend on SQLite table models; use application entities, contracts, or view models instead.

Barcode data follows this path: the Domain `Barcode` value object holds a trimmed decoded value and `BarcodeSymbology`; the Application exposes typed global owner lookup and assignment services; SQLite and JSON rows persist the value and nullable symbology; backup/export and restore preserve both fields. Keep matching trim-only and case-sensitive. Treat values as globally unique across container and item owners, and cover shared contract changes with SQLite/JSON parity tests. ZXing camera/gallery decoding and rendered barcode previews belong only in the MAUI project.

The current SQLite schema is defined directly by the classes in `Infrastructure/DatabaseModels`. Mothball is still in development and does not maintain SQLite migration compatibility: after a schema-breaking model change, reset the local application database before testing. Do not add startup backfills or repair scans unless upgrade compatibility becomes an explicit product requirement. Constraints and indexes that belong to the current schema should be declared on the SQLite models and covered by integration tests.

## Dependency Injection and Backend Selection

`MauiProgram.CreateMauiApp()` is the composition root. It configures the app, then calls these registration methods:

```csharp
builder.Services
    .AddCoreApplication()
    .AddPersistence(builder.Configuration)
    .AddPlatformServices()
    .AddViewModels();
```

Use the matching method when you add a service:

- `AddCoreApplication` for application handlers and cross-platform services.
- `AddPersistence` for repositories, storage services, startup initializers, and backend-specific registrations.
- `AddPlatformServices` for MAUI/device implementations.
- `AddViewModels` for page view models.

SQLite is the default backend. Set `MOTHBALL_PERSISTENCE_BACKEND=Json` to exercise the JSON operational store. `JsonOperationalStore` is accepted as an alternative value.

## Testing

Tests use NUnit and Moq. Prefer focused tests beside similar existing tests:

- Core feature behavior: `tests/UnitTests/*ServiceTests.cs` or workflow/handler tests.
- SQLite and repository behavior: repository integration and SQLite restore tests.
- JSON backend behavior: JSON operational store and JSON restore tests.
- View model or UI utility behavior: targeted view model and utility tests.

The test projects reference the `net10.0` application and persistence projects directly. Unit tests still link selected mobile-only source files so those classes can be tested without targeting iOS or Mac Catalyst; shared MAUI stubs live in `tests/Tests.Shared`. When adding a test for a new MAUI-project class, first check whether an existing abstraction can be tested from `CoreApp.Application`; only link the minimal source files needed when that is not possible.

Run the focused test file while iterating, then run the test solution filter before submitting a change:

```bash
dotnet test Mothball.Tests.slnf -v minimal
```

## Contributor Checklist

Before considering a feature complete:

- Keep the change in the correct project boundary.
- Register new services, view models, and navigation routes.
- Support both persistence backends when the behavior is backend-independent.
- Add focused tests, including backend parity where appropriate.
- Update backup/restore behavior if the data must move between devices.
- Run the relevant tests and build the app for the target platform you changed.
- Update user-facing documentation when the feature changes how the app is used.

For a feature-by-feature implementation map and algorithm overviews, see [Features and Algorithms](FeaturesAndAlgorithms.md). For deeper information on specific areas, see [Backup and Restore](BackupRestore.md), [JSON Operational Store](JsonStore.md), [Debouncer](Debouncer.md), and [Seeding](Seeding.md).
