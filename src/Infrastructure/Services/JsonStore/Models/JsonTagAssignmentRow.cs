using CoreApp.Application.Contracts.Tags;

namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonTagAssignmentRow
{
    public int Id { get; set; }
    public Guid TagId { get; set; }
    public Guid TargetId { get; set; }
    public TagTargetType TargetType { get; set; }
}
