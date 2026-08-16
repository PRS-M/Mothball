using MothballMobile.Infrastructure;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Utilities;

public sealed class TaskExtensionsTests
{
    [Test]
    public async Task FireAndForget_WhenTaskFaults_ReportsFailureToObserver()
    {
        var observer = new RecordingBackgroundTaskObserver();
        var exception = new InvalidOperationException("boom");

        Task.FromException(exception).FireAndForget(observer, "Test operation");

        var failure = await observer.Failure.Task.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Multiple(() =>
        {
            Assert.That(failure.OperationName, Is.EqualTo("Test operation"));
            Assert.That(failure.Exception, Is.SameAs(exception));
        });
    }

    [Test]
    public void FireAndForget_WhenObserverIsNull_Throws()
    {
        var task = Task.CompletedTask;

        Assert.That(
            () => task.FireAndForget(null!, "Test operation"),
            Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("observer"));
    }

    private sealed class RecordingBackgroundTaskObserver : IBackgroundTaskObserver
    {
        public TaskCompletionSource<(string OperationName, Exception Exception)> Failure { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnFailure(string operationName, Exception exception)
            => Failure.TrySetResult((operationName, exception));
    }
}
