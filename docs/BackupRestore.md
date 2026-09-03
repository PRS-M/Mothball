# Backup and Restore

This document describes the backup/export and restore features added for inventory data, including incremental restore behavior for already populated databases.

## Goals

- Provide a backend-agnostic backup payload format.
- Support restore into existing data without duplicating records.
- Keep SQLite restore atomic (all-or-nothing) when using the SQLite backend.
- Add configurable conflict handling policies.
- Add payload integrity verification before restore.

## Main Components

- `CoreApp.Application.Contracts.InventoryBackupEnvelope`
- `CoreApp.Application.Contracts.InventoryBackupData`
- `CoreApp.Application.Contracts.InventoryBackupIntegrity`
- `CoreApp.Application.Contracts.InventoryBackupRestoreOptions`
- `CoreApp.Application.Contracts.InventoryBackupRestoreResult`
- `CoreApp.Application.Features.Backup.Export.IInventoryBackupExporter`
- `CoreApp.Application.Features.Backup.Export.IInventoryBackupService`
- `CoreApp.Application.Features.Backup.Restore.IInventoryBackupRestoreService`
- `CoreApp.Application.Features.Backup.Export.InventoryBackupExporter`
- `CoreApp.Application.Features.Backup.Export.InventoryBackupService`
- `CoreApp.Application.Features.Backup.Restore.InventoryBackupRestoreService`
- `CoreApp.Application.Features.Backup.Restore.Planning.InventoryBackupRestorePlanner`
- `Infrastructure.Services.Restore.SqliteInventoryBackupRestoreService`

## Backup Export

Exporter behavior:

- Reads containers and items through query repositories.
- Includes containers, items, item-container relations, image references, and optional barcode value/symbology fields on containers and items.
- Emits a versioned envelope (`PayloadVersion`, `SchemaVersion`, `CreatedUtc`, `Source`).
- Computes and attaches integrity checksum metadata.
- Can emit either raw JSON or a ZIP archive containing the JSON payload plus photo files.

Resulting payload shape:

- `integrity`: checksum (and optional signature metadata fields).
- `data.containers`
- `data.items`
- `data.relations`
- `data.images`

Barcode fields are part of existing container and item entries. They do not change `PayloadVersion` or `SchemaVersion`, which remain `1`; absent barcode fields restore as no barcode.

ZIP archive layout:

- `backup.json`: the same JSON envelope produced by the JSON export path.
- `images/items/{imageId}.jpg`: item photo files that exist in app storage.
- `images/containers/{imageId}.jpg`: container photo files that exist in app storage.

Missing photo files are skipped so a backup can still be created when metadata references a file that is no longer present.

ZIP import behavior:

- Reads `backup.json` from the archive and applies the existing restore planner/service with the selected conflict policy.
- Restores photo files from `images/items/` and `images/containers/` into the app photo folders after metadata restore completes.
- Skips archive photo entries that have no matching image reference owner in `backup.json`.
- Leaves JSON import/export available for metadata-only backup workflows.

## Restore Implementations

### Backend-agnostic restore

`CoreApp.Application.Features.Backup.Restore.InventoryBackupRestoreService`:

- Uses repository abstractions only.
- Works for all backends.
- Executes planned operations using command repositories.

### SQLite transactional restore

`Infrastructure.Services.Restore.SqliteInventoryBackupRestoreService`:

- Uses direct SQLite models/connection.
- Runs restore operations inside one SQLite transaction.
- Ensures atomic restore (rollback on failure).

## Shared Planner

`CoreApp.Application.Features.Backup.Restore.Planning.InventoryBackupRestorePlanner` centralizes common logic:

- JSON parsing.
- Payload version validation.
- Integrity validation.
- Incremental merge planning.
- Conflict policy handling.

This removes duplicate decision logic between the generic and SQLite restore services.

## Conflict Policies

`InventoryBackupConflictPolicy` currently supports:

- `AddOnly`
- `AddAndUpsertMetadata`
- `FullSync`
- `StrictFullSync`

### AddOnly

- Inserts only missing containers/items.
- Inserts missing relation quantity deltas.
- Inserts missing images.
- Existing rows are not updated.

### AddAndUpsertMetadata

- Same as AddOnly for inserts.
- Updates existing container metadata (`Name`, `Notes`) when changed.
- Updates existing item metadata (`Name`, `Description`) when changed.

### FullSync

- Includes AddAndUpsertMetadata behavior.
- Deletes containers not present in backup.
- Deletes items not present in backup.
- Keeps relation/image restore additive for surviving entities by adding only missing quantity or missing image refs.

### StrictFullSync

- Includes FullSync root-entity behavior.
- Reconciles surviving relation pairs exactly:
- Missing pairs are inserted.
- Extra pairs are deleted.
- Existing pair quantity is set to exact backup quantity (can increase or decrease).
- Reconciles surviving image references exactly:
- Missing owner-image refs are inserted.
- Extra owner-image refs are deleted.

## Integrity Verification

`InventoryBackupIntegrity` supports:

- Checksum algorithm: `SHA256`.
- Payload checksum: required by default at restore time.
- Optional signature metadata (`HMAC-SHA256`) with optional key id.

Restore-time verification:

- Missing integrity metadata fails by default.
- Unsupported checksum/signature algorithm fails restore.
- Checksum mismatch fails restore.
- If signature is present, a signature secret is required and verified.

Options used at restore:

- `RequireIntegrityValidation` (default: true)
- `SignatureSecret` (used when signature exists)

## Restore Result Counters

Restore reports include:

- Added: containers, items, relations, relation quantity, images.
- Updated: containers, items.
- Deleted: containers, items.
- Deleted: relations, images (StrictFullSync).
- Skipped: existing containers/items/relations/images, invalid relations, images with missing owner.

## Dependency Injection Wiring

Persistence-based selection:

- JSON backend: uses `InventoryBackupRestoreService`.
- SQLite backend: uses `SqliteInventoryBackupRestoreService`.

## Test Coverage Added/Updated

- Exporter includes integrity metadata checks.
- Generic restore tests cover:
- Incremental add-only behavior.
- Metadata upsert policy.
- FullSync root-entity deletion behavior.
- Invalid JSON handling.
- Checksum mismatch rejection.
- SQLite restore tests cover:
- Transaction rollback behavior on failure.
- Incremental restore behavior.

## Notes

- `FullSync` remains root-level strict for compatibility.
- Use `StrictFullSync` when you need exact relation and image reconciliation for surviving roots.

## File Export, Sharing, and Import

The Settings feature provides both app-local and external-file workflows for JSON and ZIP backups.

- Exports are stored in the app's `Backups` folder with UTC timestamped names such as `mothball-backup-20260820-120000Z.json`.
- A selected local JSON or ZIP backup can be shared using the platform share sheet.
- Import can read a selected backup from the app's `Backups` folder or use the device file picker to import an external `.json` or `.zip` file.
- Both import paths call the same restore services, so integrity checks, optional HMAC verification, conflict policy selection, and result reporting do not vary by file origin.

The Settings view model selects the restore policy before it reads a backup file. The workflow service then supplies the configured signing secret when backup signing is enabled and dispatches content to JSON or ZIP restore as appropriate.

The in-app Documentation / Help page explains the user-facing effect of each conflict policy. The full developer policy model is described below.

## Restore Examples

Below are common usage patterns for `IInventoryBackupRestoreService`.

### 1. Default restore (AddOnly + required checksum validation)

```csharp
var backup = await backupExporter.ExportAsync();
var result = await backupRestoreService.RestoreAsync(backup);
```

Equivalent JSON path:

```csharp
var result = await backupRestoreService.RestoreFromJsonAsync(backupJson);
```

Behavior:

- Missing rows are inserted.
- Existing rows are not updated.
- Checksum metadata is required and verified.

### 2. Upsert metadata when entities already exist

```csharp
var options = new InventoryBackupRestoreOptions
{
	ConflictPolicy = InventoryBackupConflictPolicy.AddAndUpsertMetadata,
};

var result = await backupRestoreService.RestoreAsync(backup, options);
```

Behavior:

- Missing rows are inserted.
- Existing containers/items are updated when metadata differs.
- Relations/images remain incremental (missing-only).

### 3. FullSync for root entities

```csharp
var options = new InventoryBackupRestoreOptions
{
	ConflictPolicy = InventoryBackupConflictPolicy.FullSync,
};

var result = await backupRestoreService.RestoreAsync(backup, options);
```

Behavior:

- Add + metadata upsert behavior.
- Containers/items not present in backup are deleted.
- Relations/images for surviving roots are still additive in current implementation.

### 3b. StrictFullSync for exact graph reconciliation

```csharp
var options = new InventoryBackupRestoreOptions
{
	ConflictPolicy = InventoryBackupConflictPolicy.StrictFullSync,
};

var result = await backupRestoreService.RestoreAsync(backup, options);
```

Behavior:

- FullSync root behavior.
- Exact relation reconciliation for surviving roots (insert/delete/set quantity).
- Exact image reference reconciliation for surviving roots (insert/delete).

### 4. Signed payload verification (HMAC)

When creating a signed backup:

```csharp
var signed = InventoryBackupRestorePlanner.AttachIntegrity(
	backup,
	signatureSecret: "my-shared-secret",
	keyId: "device-key-v1");
```

When restoring a signed backup:

```csharp
var options = new InventoryBackupRestoreOptions
{
	SignatureSecret = "my-shared-secret",
};

var result = await backupRestoreService.RestoreAsync(signed, options);
```

Behavior:

- Checksum is verified first.
- If signature is present, HMAC signature is verified using `SignatureSecret`.
- Missing/invalid secret for signed payload fails restore.

### 5. Allow restore without integrity metadata (not recommended)

```csharp
var options = new InventoryBackupRestoreOptions
{
	RequireIntegrityValidation = false,
};

var result = await backupRestoreService.RestoreAsync(backupWithoutIntegrity, options);
```

Behavior:

- Restore can proceed when integrity metadata is absent.
- Use only for legacy migrations or controlled offline workflows.
