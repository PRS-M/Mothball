using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure.BackgroundOperations.Observability;

public sealed class LoggingBackgroundTaskObserver : IBackgroundTaskObserver
{
    private readonly ILogger<LoggingBackgroundTaskObserver> logger;

    public LoggingBackgroundTaskObserver(ILogger<LoggingBackgroundTaskObserver> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void OnFailure(string operationName, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        logger.LogError(
            exception,
            "Background operation {OperationName} failed.",
            string.IsNullOrWhiteSpace(operationName) ? "Unnamed operation" : operationName);
    }
}
