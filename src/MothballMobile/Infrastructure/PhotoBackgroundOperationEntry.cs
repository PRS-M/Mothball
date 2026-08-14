namespace MothballMobile.Infrastructure;

public sealed class PhotoBackgroundOperationEntry
{
    public required string Description { get; init; }
    public required bool Succeeded { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public required double FinalProgress { get; init; }
}
