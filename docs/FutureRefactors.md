# Future Refactors

This document is a lightweight register for worthwhile improvements that are understood but do not yet justify a separate issue tracker entry. Each entry should describe the current trade-off, the intended end state, and the evidence needed to complete the work.

## Register

### Persist an explicit image display order

**Status:** Proposed  
**Area:** SQLite and JSON persistence, backup/restore, image presentation

The application treats the first image in an aggregate's photo collection as its default image. JSON stores an explicit `RowId` and orders image rows by that value. SQLite currently has only a GUID primary key on `DbImage`, so the repositories use `ORDER BY rowid` to preserve insertion order in the current database. This keeps both backends aligned, but SQLite's implicit `rowid` is not a durable application-level ordering field; SQLite documents that it can change during `VACUUM` when it is not aliased by an `INTEGER PRIMARY KEY`.

The durable design is to add an explicit image sequence or creation-order field, for example `SortOrder` or `CreatedSequence`, to the SQLite model and the JSON image row. New images should receive the next value for their owner. Backup export and restore should preserve the field, and existing databases need a migration/backfill that follows their current insertion order.

Completion should include:

- SQLite schema migration and JSON model/state updates.
- Domain/application mapping where the order is exposed.
- Export and restore parity for image order.
- Tests proving the first inserted image remains the default after reload, backup/restore, and both persistence backends.
- Removal of the `rowid` dependency from repository queries.

References: [SQLite rowid tables](https://www.sqlite.org/rowidtable.html), [sqlite-net query ordering](https://github.com/praeclarum/sqlite-net).

### Return image ordering to mapped LINQ queries

**Status:** Follows “Persist an explicit image display order”  
**Area:** SQLite repository query style

Once image order is represented by a mapped, durable property, replace the current image-specific SQL ordering with sqlite-net LINQ such as:

```csharp
var photos = await database.Connection
    .Table<DbImage>()
    .Where(image => image.OwnerUniqueId == ownerId)
    .OrderBy(image => image.SortOrder)
    .ToListAsync();
```

This keeps the ordering rule visible in the model and lets the repository use the same strongly typed query style already supported by sqlite-net. The current raw SQL is intentionally retained until there is a real mapped ordering property; ordering by `ImageId` would be deterministic but would not mean “first photo added.”

## Entry template

When adding an item, use this shape:

```markdown
### Short improvement name

**Status:** Proposed | Planned | In progress | Complete  
**Area:** Affected layers or feature

What is awkward today, and why it matters.

Describe the intended end state, compatibility concerns, and migration work.

Completion should include:

- Concrete implementation work.
- Tests or other verification.
- Documentation or backup/parity updates where applicable.
```

