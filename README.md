# Mothball

## Test status

[![PR Tests](https://github.com/PRS-M/Mothball/actions/workflows/pr-tests.yml/badge.svg)](https://github.com/PRS-M/Mothball/actions/workflows/pr-tests.yml)

## Overview
Mothball helps you remember what you have, where you put it, and how much is left. Use it for a garage, workshop, pantry, wardrobe, storage unit, or small stockroom: create bins, shelves, drawers, or cabinets, then add the items inside.

Add notes, photos, and quantities to make things easy to find later. Your inventory stays on your device and can be backed up and restored when needed.

<p align="center">
  <video src="docs/MothballOverview_iPhone%2016%20Pro%20-%202026-08-14.mp4" controls width="360">
    <a href="docs/MothballOverview_iPhone%2016%20Pro%20-%202026-08-14.mp4">Overview video</a>
  </video>
</p>

## What the app does

- Organize things into containers and items.
- Associate items to containers with quantity tracking.
- Add and manage image references for containers and items.
- Search and browse data with paged list workflows optimized for mobile UI responsiveness.
- Start reliably with backend initialization and recovery hooks.

## Practical uses

### Home and garage catalog

Catalog storage bins, shelving units, cupboards, and drawers so you can find seasonal decorations, tools, cables, camping gear, spare hardware, and household supplies without opening every box. A container can represent a physical bin, a shelf section, a wardrobe, or an entire cupboard.

### Workshop, hobby, and craft supplies

Track consumables such as screws, sandpaper, paint, filament, fabric, electronics components, or model-making supplies. Record the same item in more than one container when stock is split across a workbench, a parts cabinet, and overflow storage, then keep an accurate quantity for each location.

### Wardrobes and seasonal storage

Create containers for wardrobes, drawers, vacuum bags, or under-bed storage. Add descriptions and photos for clothing, shoes, accessories, and seasonal items so they are easy to identify when packed away.

### Small stockroom inventory

Use shelves, bins, cabinets, or zones as containers and treat products, spare parts, rental equipment, or office supplies as items. Mothball gives a small team or sole operator a clear view of where stock is kept and how much is available, without the overhead of a full warehouse system.

## Core capabilities

### Inventory management

- Create, edit, and list containers.
- Create, edit, and list items.
- Associate existing items with containers.
- Track relation quantities between items and containers.
- Scan, label, and find containers and item types by barcode.

### Backup and restore

- Export a versioned inventory payload including containers, items, relations, and images.
- Validate payload integrity (SHA256 checksum, optional HMAC metadata).
- Restore with configurable conflict policies:
  - `AddOnly`
  - `AddAndUpsertMetadata`
  - `FullSync`
  - `StrictFullSync`
- Use backend-agnostic restore or SQLite transactional restore (all-or-nothing behavior).

### Barcodes

- Assign an optional barcode to each container or item type and display it as a scannable label.
- Scan with the device camera or decode a barcode from a selected image.
- Use Scan from the container list, item list, or app toolbar to open the matching record.
- Prevent duplicate barcode assignments across all containers and item types.
- Use item barcode scans to receive more quantity for an existing item, optionally into a scanned container.

See [Barcodes](docs/Barcodes.md) for barcode workflows, supported formats, and data rules.

### Persistence backends

- SQLite backend for device-local persistence.
- JSON operational store backend with two-slot commit strategy, manifest generations, and rollback/recovery support.
- Backend selection through configuration/environment.

### UX and reliability features

- Debounced search/input behavior to avoid expensive repeated queries during typing bursts.
- Development-time demo data seeding in Debug builds.
- Unit tests across core services, persistence behavior, mapper behavior, startup orchestration, and restore planning.

## Project structure

- `src/CoreApp.Domain`: domain entities, aggregate markers, and format-independent inventory policies.
- `src/CoreApp.Application`: use cases, contracts, ports, specifications, backup workflows, and platform-independent utilities.
- `src/Infrastructure`: persistence-focused infrastructure implementations.
- `src/Infrastructure.Platform.Maui`: MAUI platform-specific infrastructure services.
- `src/MothballMobile`: .NET MAUI app project (UI, composition, app startup).
- `tests/UnitTests`: unit and integration-style tests for app behavior.

## Documentation index

- [Developer Documentation](docs/DeveloperDocumentation.md)
  - Explains the solution structure, project boundaries, extension points, dependency injection, and testing for new contributors.
- [Features and Algorithms](docs/FeaturesAndAlgorithms.md)
  - Maps user features to their implementation and summarizes the main inventory, persistence, backup, navigation, and UI algorithms.
- [Barcodes](docs/Barcodes.md)
  - Describes barcode scanning, assignment, lookup, item receipts, supported formats, data rules, and persistence behavior.
- [Overview video](<docs/MothballOverview_iPhone 16 Pro - 2026-08-14.mp4>)
  - Shows the app running on an iPhone 16 Pro simulator.
- [Backup and Restore](docs/BackupRestore.md)
  - Explains export format, restore strategies, conflict policies, integrity checks, and usage examples.
- [Debouncer](docs/Debouncer.md)
  - Describes the concurrency utility used for trailing-edge debounce behavior in UI workflows.
- [Localization](docs/Localization.md)
  - Documents English/Polish resources, language preference lifecycle, platform declarations, and the restart-after-selection behavior.
- [JSON Operational Store](docs/JsonStore.md)
  - Details the JSON backend layout, commit algorithm, slot/manifest strategy, and recovery logic.
- [Seeding](docs/Seeding.md)
  - Documents development-only demo data seeding triggers, safety rules, and current seeded-entity marker behavior.
- [AdMob Configuration](docs/AdMobConfiguration.md)
  - Explains Debug test IDs and the local/CI Release configuration process.

## Supported platforms

`src/MothballMobile/MothballMobile.csproj` targets:

- `net10.0-ios`
- `net10.0-maccatalyst`
- `net10.0-windows10.0.19041.0` (conditionally included on Windows)

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

For fast local app verification on macOS, build the iOS target explicitly:

```bash
dotnet build src/MothballMobile/MothballMobile.csproj -f net10.0-ios --no-restore
```

## Run tests

Run unit tests:

```bash
dotnet test tests/UnitTests/UnitTests.csproj -v minimal
```

Run integration tests:

```bash
dotnet test tests/IntegrationTests/IntegrationTests.csproj -v minimal
```

Or use the test-only solution filter:

```bash
dotnet test Mothball.Tests.slnf -v minimal
```

For a full solution verification from a clean or stale restore state, let the solution restore first:

```bash
dotnet test Mothball.sln
```

Use `--no-restore` only after a successful solution restore:

```bash
dotnet test Mothball.sln --no-restore
```

The solution includes MAUI iOS and Mac Catalyst projects. On Apple Silicon, Mac Catalyst uses the active `maccatalyst-arm64` runtime, so a project-only restore for iOS is not enough for `dotnet test Mothball.sln --no-restore`.

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
- `Mothball.Tests.slnf` for unit-test-only solution-level verification

Projects included:

- `CoreApp.Domain`
- `CoreApp.Application`
- `Infrastructure.Persistence`
- `Infrastructure.Platform.Maui`
- `MothballMobile`
- `UnitTests`

## Hashtags
***Keywords**: `WMS, WarehouseManagementSystem, Warehouse, House, Organization, Catalogue, Catalog, MAUI, dotNET, .NET, iOS, Apple`*
