namespace MothballMobile.Infrastructure.Resilience;

/// <summary>
/// Defines debounced execution of asynchronous operations.
/// </summary>
public interface IDebouncer
{
    /// <summary>
    /// Delays execution of an operation until the debounce interval has elapsed.
    /// </summary>
    /// <param name="action">The value used by the operation.</param>
    /// <param name="cancellationToken">A token for cancelling the operation.</param>
    Task DebounceAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
