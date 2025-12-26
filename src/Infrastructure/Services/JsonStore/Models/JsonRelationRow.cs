using System;

namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonRelationRow
{
    // Mirrors DbItemContainerRelation.Id (AUTOINCREMENT) for stable paging and "first relation" selection.
    public int Id { get; set; }
    public Guid ItemId { get; set; }
    public Guid ContainerId { get; set; }
    public int Quantity { get; set; }
}
