namespace Infrastructure.Services.JsonStore.Models;

public sealed class JsonStoreCommitInfo
{
    public int Generation { get; set; }
    public Guid CommitId { get; set; }
    public DateTimeOffset CommittedUtc { get; set; }
}
