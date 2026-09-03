# Barcodes

Mothball can identify containers and item types with barcodes. A barcode lets you open the matching record quickly, print or display a label for a stored record, and receive more quantity for an item without searching the inventory manually.

## What You Can Do

- Assign one optional barcode to a container or item type when creating it or from its details page.
- Display an assigned barcode as a rendered label on container and item details pages.
- Scan a barcode with the device camera or select an image containing a barcode from the photo library.
- Start Scan from the app toolbar, item list, or container list to open the matching item or container.
- Scan an item barcode in the Add Item workflow to receive additional quantity for that existing item.
- Scan a container barcode while receiving or associating an item to use that container as the destination.

## Supported Formats

QR Code is always available. In Settings, enable **Barcodes extended mode** to select and scan the additional formats supported by the device barcode reader. The scanner only accepts formats enabled by the current setting.

The details pages render an assigned value using its recorded symbology, so the displayed label matches the type that was scanned or selected.

## Assigning and Editing

Use the Barcode field on an add form to enter a value manually or use **Scan Barcode**. On an item or container details page, select the edit control beside Barcode to enter, replace, clear, or scan a value.

Each barcode value belongs to at most one inventory record across the whole app. A value assigned to a container cannot also be assigned to an item, and vice versa. Reusing the same owner's current value is allowed. If a value is already in use, Mothball keeps the current record unchanged and shows a native device alert.

Values are trimmed before they are saved or looked up. Matching is case-sensitive: `BOX-01` and `box-01` are different values.

## Finding a Record

Select **Scan Barcode** from the toolbar or either inventory list, then scan with the camera or choose a source image. When Mothball finds an assigned value, it opens the matching container or item details page. Cancelling the scanner makes no changes.

If the scanned barcode is not assigned, the scan completes without opening a record. Scanner and lookup failures are shown in a native device alert.

## Receiving an Existing Item

In the Add Item workflow, entering or scanning a barcode that already belongs to an item switches the page to receipt mode. The existing item's metadata is preserved, and saving adds the requested quantity instead of creating a second item.

In simple mode, the receipt quantity defaults to one. In advanced mode, enter a positive quantity. The received quantity can remain unassigned, go to the container that opened the form, or go to a container found by scanning its barcode. A barcode owned by a container does not enter item receipt mode; it remains unavailable for a new item assignment.

## Data and Backup Behavior

Mothball stores the decoded barcode value and its symbology, not the camera image or source image used to scan it. Barcode values and symbologies are persisted by both the SQLite and JSON backends. Backup export and restore retain those fields, so an assigned barcode moves with its container or item when inventory data is restored.

For contributors, barcode ownership is enforced by the Application-layer `IBarcodeAssignmentService` and creation handlers. Lookup uses `IInventoryQueryRepository.FindBarcodeAsync`. Preserve the trim-only, case-sensitive matching and global uniqueness rules when changing either persistence backend or backup/restore behavior. Camera/gallery decoding and barcode rendering are MAUI-only concerns.