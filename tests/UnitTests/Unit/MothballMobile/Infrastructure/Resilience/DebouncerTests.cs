using MothballMobile.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests;

public class DebouncerTests
{
    [Test]
    public async Task Debounce_ExecutesOnlyOnce_WhenCalledMultipleTimesQuickly()
    {
        var debouncer = new Debouncer(50, NullLogger<Debouncer>.Instance);
        int count = 0;

        _ = debouncer.DebounceAsync(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });
        _ = debouncer.DebounceAsync(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });
        _ = debouncer.DebounceAsync(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });

        await Task.Delay(120);
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Debounce_CanBeDisposed_SuppressesFurtherExecutions()
    {
        var debouncer = new Debouncer(50, NullLogger<Debouncer>.Instance);
        int count = 0;

        _ = debouncer.DebounceAsync(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });
        debouncer.Dispose();
        // After dispose, new requests should be ignored/canceled
        _ = debouncer.DebounceAsync(_ =>
        {
            Interlocked.Increment(ref count);
            return Task.CompletedTask;
        });
        await Task.Delay(100);
        Assert.That(count, Is.LessThanOrEqualTo(1));
    }
}
