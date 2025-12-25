using System;

namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonImageRow
{
    public int RowId { get; set; }
    public Guid ImageId { get; set; }
    public Guid OwnerUniqueId { get; set; }

    // Present in SQLite schema, unused in current domain behavior.
    public string? ImageDataBase64 { get; set; }
}
