# Copilot Cloud Agent Instructions

Trust `AGENTS.md` as the repository-wide guide. Use this file for the cloud-agent-specific operating details below, and search the repository only when these instructions are incomplete or demonstrably stale.

## Fast Start

Work from the repository root. This is a .NET 10 MAUI solution, not a Node or Python project. The cloud environment may not provide `rg`; use the paths in `AGENTS.md`, `README.md`, and the docs index before broad code search. Do not spend time inspecting `bin/`, `obj/`, `.vs/`, or the checked-in `maui-debug.binlog`.

Before editing, identify the owning layer and nearest existing test. After editing, run the narrowest relevant test or build immediately. Before a pull request, always run the CI-equivalent sequence:

```bash
dotnet restore Mothball.Tests.slnf
dotnet test Mothball.Tests.slnf --no-restore --configuration Release --verbosity minimal
```

This sequence has been verified on macOS with .NET SDK `10.0.302` and passes 315 tests. The GitHub workflow is `.github/workflows/pr-tests.yml` and performs the same restore/test operations on Ubuntu with .NET `10.0.x`.

## Build Facts

For a source/solution build use `dotnet build Mothball.sln`; for quick app validation use `dotnet build src/MothballMobile/MothballMobile.csproj -f net10.0-maccatalyst --no-restore` after restore. The app targets `net10.0-ios` and `net10.0-maccatalyst`, plus Windows only on Windows hosts. A full solution build may spend several minutes in iOS simulator AOT after Mac Catalyst has already succeeded. A short cloud command timeout is therefore not evidence of a compile error; use a platform-specific build or a longer-running command when iOS output is specifically relevant. Existing build warnings include an unused `BasePage` field and missing `x:DataType` suggestions in a few XAML bindings.

There is no lint command configured. `Directory.Packages.props` owns all package versions. Never add per-project package versions unless deliberately handling the documented MAUI DevFlow/CPM exception. Do not use `--no-restore` after changing project/package files until restore succeeds.

## High-Risk Repository Conventions

- Keep Domain independent and Application platform/persistence agnostic.
- When changing persistence behavior, implement and test both SQLite and JSON paths where the contract is shared.
- Unit tests cannot project-reference the MAUI-targeted app; link only the required mobile source files in `tests/UnitTests/UnitTests.csproj`.
- MAUI DevFlow can interact with Central Package Management; preserve the existing conditional `ManagePackageVersionsCentrally=false` and explicit package updates used for Debug DevFlow builds.
- SQLite schema changes have no migration compatibility in this development-stage app. Reset local app data for a breaking schema change rather than inventing a backfill.
- Debug seeding is startup-only and recognizes the exact seed marker; never broaden it to user-created containers.
- Localization changes require matching keys in English, Polish, German, and Spanish resources, with German and Spanish retaining their AI-translated status.
- Release iOS builds need ignored `AdMob.Release.props` and `appsettings.Release.json` created from examples. Do not commit them or secrets.

Use focused tests for planner/domain/application changes, integration tests for persistence and backend parity, and targeted MAUI builds for UI/project changes. Update the relevant documentation when behavior or developer workflow changes. Leave generated outputs and unrelated working-tree changes untouched.