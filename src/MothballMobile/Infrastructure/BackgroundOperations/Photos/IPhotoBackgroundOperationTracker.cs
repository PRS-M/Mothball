namespace MothballMobile.Infrastructure.BackgroundOperations.Photos;

public interface IPhotoBackgroundOperationTracker
{
    int ActiveOperationCount { get; }
    double OverallProgress { get; }
    string StatusText { get; }
    bool IsBannerVisible { get; }
    IReadOnlyList<PhotoBackgroundOperationEntry> RecentOperations { get; }

    Guid Start(string operationDescription);
    void Report(Guid operationId, double progress);
    void Complete(Guid operationId, bool success);
}
