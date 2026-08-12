# Backup and Restore

This document describes the backup/export and restore features added for inventory data, including incremental restore behavior for already populated databases.

## Goals

- Provide a backend-agnostic backup payload format.
- Support restore into existing data without duplicating records.
- Keep SQLite restore atomic (all-or-nothing) when using the SQLite backend.
- Add configurable conflict handling policies.
- Add payload integrity verification before restore.

## Main Components

- `CoreApp.Contracts.InventoryBackupEnvelope`
- `CoreApp.Contracts.InventoryBackupData`
- `CoreApp.Contracts.InventoryBackupIntegrity`
- `CoreApp.Contracts.InventoryBackupRestoreOptions`
- `CoreApp.Contracts.InventoryBackupRestoreResult`
- `CoreApp.Interfaces.IInventoryBackupExporter`
- `CoreApp.Interfaces.IInventoryBackupService`
- `CoreApp.Interfaces.IInventoryBackupRestoreService`
- `CoreApp.Services.InventoryBackupExporter`
- `CoreApp.Services.InventoryBackupService`
- `CoreApp.Services.InventoryBackupRestoreService`
- `CoreApp.Utilities.InventoryBackupRestorePlanner`
- `Infrastructure.Services.Restore.SqliteInventoryBackupRestoreService`

## Backup Export

Exporter behavior:

- Reads containers and items through query repositories.
- Includes containers, items, item-container relations, and image references.
- Emits a versioned envelope (`PayloadVersion`, `SchemaVersion`, `CreatedUtc`, `Source`).
- Computes and attaches integrity checksum metadata.

Resulting payload shape:

- `integrity`: checksum (and optional signature metadata fields).
- `data.containers`
- `data.items`
- `data.relations`
- `data.images`

## Restore Implementations

### Backend-agnostic restore

`CoreApp.Services.InventoryBackupRestoreService`:

- Uses repository abstractions only.
- Works for all backends.
- Executes planned operations using command repositories.

### SQLite transactional restore

`Infrastructure.Services.Restore.SqliteInventoryBackupRestoreService`:

- Uses direct SQLite models/connection.
- Runs restore operations inside one SQLite transaction.
- Ensures atomic restore (rollback on failure).

## Shared Planner

`CoreApp.Utilities.InventoryBackupRestorePlanner` centralizes common logic:

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

- `FullSync` currently enforces strict sync at root entity level (containers/items).
- For surviving roots, relation and image handling remains additive, not strict replacement.
- Strict full graph reconciliation can be added in a future iteration if needed.

## How to Use Restore

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
