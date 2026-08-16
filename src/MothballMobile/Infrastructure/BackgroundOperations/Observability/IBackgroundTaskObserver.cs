namespace MothballMobile.Infrastructure.BackgroundOperations.Observability;

public interface IBackgroundTaskObserver
{
    void OnFailure(string operationName, Exception exception);
}
