# Mothball

Mothball is a .NET MAUI inventory app focused on organizing household or workshop storage.
You can manage containers and items, track quantities, attach photos, and keep your data portable with backup/restore.

## What the app does

- Organize things into containers and items.
- Associate items to containers with quantity tracking.
- Add and manage image references for containers and items.
- Search and browse data with paged list workflows optimized for mobile UI responsiveness.
- Start reliably with backend initialization and recovery hooks.

## Core capabilities

### Inventory management

- Create, edit, and list containers.
- Create, edit, and list items.
- Associate existing items with containers.
- Track relation quantities between items and containers.

### Backup and restore

- Export a versioned inventory payload including containers, items, relations, and images.
- Validate payload integrity (SHA256 checksum, optional HMAC metadata).
- Restore with configurable conflict policies:
  - `AddOnly`
  - `AddAndUpsertMetadata`
  - `FullSync`
  - `StrictFullSync`
- Use backend-agnostic restore or SQLite transactional restore (all-or-nothing behavior).

### Persistence backends

- SQLite backend for device-local persistence.
- JSON operational store backend with two-slot commit strategy, manifest generations, and rollback/recovery support.
- Backend selection through configuration/environment.

### UX and reliability features

- Debounced search/input behavior to avoid expensive repeated queries during typing bursts.
- Development-time demo data seeding in Debug builds.
- Unit tests across core services, persistence behavior, mapper behavior, startup orchestration, and restore planning.

## Project structure

- `src/CoreApp`: domain contracts, entities, interfaces, and core services.
- `src/Infrastructure`: persistence-focused infrastructure implementations.
- `src/Infrastructure.Platform.Maui`: MAUI platform-specific infrastructure services.
- `src/MothballMobile`: .NET MAUI app project (UI, composition, app startup).
- `tests/UnitTests`: unit and integration-style tests for app behavior.

## Documentation index

- [Backup and Restore](docs/BackupRestore.md)
  - Explains export format, restore strategies, conflict policies, integrity checks, and usage examples.
- [Debouncer](docs/Debouncer.md)
  - Describes the concurrency utility used for trailing-edge debounce behavior in UI workflows.
- [JSON Operational Store](docs/JsonStore.md)
  - Details the JSON backend layout, commit algorithm, slot/manifest strategy, and recovery logic.
- [Seeding](docs/Seeding.md)
  - Documents development-only demo data seeding triggers, safety rules, and current seeded-entity marker behavior.

## Supported platforms

`src/MothballMobile/MothballMobile.csproj` targets:

- `net10.0-ios`
- `net10.0-maccatalyst`
- `net9.0-windows10.0.19041.0` (conditionally included on Windows)

Notes:

- On macOS, primary targets are iOS Simulator and Mac Catalyst.
- Windows target is enabled when building on Windows.

## Prerequisites

- .NET SDK 10.x (for MAUI targets and test project)
- MAUI workload installed (`dotnet workload install maui`)
- macOS build for iOS/Mac Catalyst:
  - Xcode installed and opened at least once
  - iOS Simulator runtime installed
- Windows build for Windows target:
  - Windows 10/11 with required Windows SDK

## Build

From repository root:

```bash
dotnet build Mothball.sln
```

Or build just the app project:

```bash
dotnet build src/MothballMobile/MothballMobile.csproj
```

## Run tests

Run unit tests:

```bash
dotnet test tests/UnitTests/UnitTests.csproj -v minimal
```

## Run the app

### Option A: VS Code / Visual Studio debug run (recommended)

- Open the solution/workspace.
- Select `MothballMobile` startup project.
- Choose target device/simulator (iOS Simulator, Mac Catalyst, or Windows machine).
- Start debugging.

### Option B: CLI (Mac Catalyst)

```bash
dotnet build -t:Run -f net10.0-maccatalyst src/MothballMobile/MothballMobile.csproj
```

### Option C: CLI (iOS Simulator)

```bash
dotnet build -t:Run -f net10.0-ios -r iossimulator-arm64 src/MothballMobile/MothballMobile.csproj
```

If your environment is not fully configured for CLI launching on iOS, run from the IDE debugger instead.

## Backend selection

Persistence backend is configuration driven in the app composition layer.

- Default backend is SQLite.
- You can override via environment variable:

```bash
MOTHBALL_PERSISTENCE_BACKEND=Json
```

Accepted JSON backend values include `Json` and `JsonOperationalStore`.

## Current solution file

Main solution:

- `Mothball.sln`

Projects included:

- `CoreApp`
- `Infrastructure.Persistence`
- `Infrastructure.Platform.Maui`
- `MothballMobile`
- `UnitTests`
