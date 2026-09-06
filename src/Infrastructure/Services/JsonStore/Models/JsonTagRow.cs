namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonTagRow
{
    public Guid TagId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
}
