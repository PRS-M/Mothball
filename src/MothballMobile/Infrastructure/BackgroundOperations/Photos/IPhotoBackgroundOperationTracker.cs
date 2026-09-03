namespace MothballMobile.Infrastructure.BackgroundOperations.Photos;

/// <summary>
/// Defines state tracking for background photo operations.
/// </summary>
public interface IPhotoBackgroundOperationTracker
{
    int ActiveOperationCount { get; }
    bool IsProcessing { get; }
    double OverallProgress { get; }
    string StatusText { get; }
    bool IsBannerVisible { get; }
    IReadOnlyList<PhotoBackgroundOperationEntry> RecentOperations { get; }

    /// <summary>
    /// Starts the .
    /// </summary>
    /// <param name="operationDescription">The value used by the operation.</param>
    Guid Start(string operationDescription);
    /// <summary>
    /// Reports the .
    /// </summary>
    /// <param name="operationId">The identifier used by the operation.</param>
    /// <param name="progress">The value used by the operation.</param>
    void Report(Guid operationId, double progress);
    /// <summary>
    /// Completes the .
    /// </summary>
    /// <param name="operationId">The identifier used by the operation.</param>
    /// <param name="success">The value used by the operation.</param>
    void Complete(Guid operationId, bool success);
}
