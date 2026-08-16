using System;

namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonItemRow
{
    public int RowId { get; set; }
    public Guid ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
