namespace MothballMobile.Infrastructure;

public interface IBackgroundTaskObserver
{
    void OnFailure(string operationName, Exception exception);
}
