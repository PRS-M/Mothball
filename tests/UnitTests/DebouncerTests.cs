using MothballMobile.Infrastructure;

namespace UnitTests;

public class DebouncerTests
{
    [Test]
    public async Task Debounce_ExecutesOnlyOnce_WhenCalledMultipleTimesQuickly()
    {
        var debouncer = new Debouncer(50);
        int count = 0;

        debouncer.Debounce(() => Interlocked.Increment(ref count));
        debouncer.Debounce(() => Interlocked.Increment(ref count));
        debouncer.Debounce(() => Interlocked.Increment(ref count));

        await Task.Delay(120);
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Debounce_CanBeDisposed_SuppressesFurtherExecutions()
    {
        var debouncer = new Debouncer(50);
        int count = 0;

        debouncer.Debounce(() => Interlocked.Increment(ref count));
        debouncer.Dispose();
        // After dispose, new requests should be ignored/canceled
        debouncer.Debounce(() => Interlocked.Increment(ref count));
        await Task.Delay(100);
        Assert.That(count, Is.LessThanOrEqualTo(1));
    }
}
