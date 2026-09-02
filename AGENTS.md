# Mothball Agent Guide

## Repository

Mothball is a .NET MAUI mobile inventory app for tracking containers, items, quantities, photos, and local backups. It supports SQLite and a JSON operational-store backend. The repository is a C#/.NET 10 solution using NUnit, Moq, CommunityToolkit.Mvvm, SQLite, SkiaSharp, and MAUI. The primary development targets on macOS are iOS and Mac Catalyst; Windows is conditionally included on Windows hosts.

The important project boundaries are:

- `src/CoreApp.Domain`: dependency-free domain entities and inventory rules.
- `src/CoreApp.Application`: use cases, contracts, repository ports, backup/restore, and platform-independent services.
- `src/Infrastructure`: SQLite and JSON persistence, repositories, mappers, startup, restore, and seeding.
- `src/Infrastructure.Platform.Maui`: camera, file, and other MAUI/device services.
- `src/MothballMobile`: MAUI pages, XAML, view models, composition, navigation, resources, and startup.
- `tests/UnitTests`: NUnit unit tests, including selected mobile sources linked directly for testability.
- `tests/IntegrationTests`: persistence, restore, and backend-parity tests.
- `tests/Tests.Shared`: shared test stubs/support.

Dependencies flow toward the core: `MothballMobile -> CoreApp.Application -> CoreApp.Domain`, with persistence and platform projects implementing application abstractions. Keep Domain free of Application, MAUI, persistence, serialization, logging, and device APIs. Keep Application free of MAUI and concrete storage APIs.

## Build and Test

Use .NET SDK 10.x and the MAUI workload. The checked-in CI workflow is `.github/workflows/pr-tests.yml`; it uses `ubuntu-latest`, .NET `10.0.x`, restores `Mothball.Tests.slnf`, then runs the Release test command.

From the repository root, the reliable validation sequence is:

```bash
dotnet restore Mothball.Tests.slnf
dotnet test Mothball.Tests.slnf --no-restore --configuration Release --verbosity minimal
```

The verified result in this environment is 315 passing tests: 53 integration tests and 262 unit tests. During iteration, run a focused test project, then repeat the full test-filter command before submitting. `dotnet test tests/UnitTests/UnitTests.csproj -v minimal` is the VS Code unit-test task; the integration project can be run similarly.

Build the complete solution with `dotnet build Mothball.sln`. For a macOS app build, use `dotnet build src/MothballMobile/MothballMobile.csproj -f net10.0-maccatalyst --no-restore` after a successful restore, or build the iOS simulator target with `-f net10.0-ios`. The solution builds Mac Catalyst for `maccatalyst-x64` and `maccatalyst-arm64`. iOS AOT can take several minutes and may outlive a short command timeout. A local full Release build previously reached successful Mac Catalyst outputs, then remained in iOS simulator AOT; do not treat a short tool timeout alone as a source failure. The build currently emits five known warnings: one unused field in `BasePage` and four XAML compiled-binding suggestions.

There is no repository lint script or formatter configuration. Treat compiler warnings and XAML diagnostics as relevant when touching their files. Do not use `--no-restore` until the corresponding solution/project has been restored. Central Package Management is enabled in `Directory.Packages.props`; package versions belong there, not on ordinary project references.

The MAUI app can be launched from the IDE using `MothballMobile` and an iOS Simulator or Mac Catalyst target. CLI options are documented in `README.md`, but device/Apple SDK availability is environment-dependent. Release iOS builds require local ignored AdMob configuration from the committed examples; never commit those files or actual credentials.

## Change Guidance

Put behavior in the lowest layer that can own it. For stored data, update the Application contract/model first, then the SQLite model/mapper/repository and equivalent JSON implementation. Consider backup export/restore and add parity tests. Register new application services, persistence services, platform services, view models, and routes in their existing composition/navigation extension points.

For UI, keep feature page, XAML, code-behind, view model, and presentation-only models together under `src/MothballMobile/UI/Features`. Prefer compiled bindings (`x:DataType`), CommunityToolkit MVVM attributes, `CollectionView` for lists, and navigation through `INavigationService`. User-facing strings belong in all localization `.resx` files and may also require `ResourceKeyMap.cs`.

Do not reference the MAUI app project from `UnitTests`. Its mobile-only classes are deliberately listed as explicit `<Compile Include=...>` entries in `tests/UnitTests/UnitTests.csproj`; add a new entry when a test needs another mobile source file. Do not add generated `bin/`, `obj/`, IDE cache, local AdMob configuration, or design-prototype files.

Read the relevant document before changing a specialized workflow: `docs/DeveloperDocumentation.md`, `docs/FeaturesAndAlgorithms.md`, `docs/BackupRestore.md`, `docs/JsonStore.md`, `docs/Seeding.md`, `docs/Localization.md`, and `docs/AdMobConfiguration.md`. Follow existing tests and preserve unrelated user changes.