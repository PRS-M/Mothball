using CoreApp.Interfaces;
using Microsoft.Extensions.Logging;
using MothballMobile.Infrastructure;

namespace UnitTests;

[TestFixture]
public class AppStartupOrchestratorTests
{
    [Test]
    public async Task StartAsync_WhenInitializerSucceeds_CompletesWithoutError()
    {
        var initializer = new TestStartupInitializer(shouldThrow: false);
        var logger = new CapturingLogger<AppStartupOrchestrator>();
        var orchestrator = new AppStartupOrchestrator(initializer, logger);

        await orchestrator.StartAsync();

        Assert.That(initializer.Calls, Is.EqualTo(1));
        Assert.That(logger.Entries.Any(e => e.Level == LogLevel.Error), Is.False);
    }

    [Test]
    public void StartAsync_WhenInitializerFails_LogsAndRethrows()
    {
        var initializer = new TestStartupInitializer(shouldThrow: true);
        var logger = new CapturingLogger<AppStartupOrchestrator>();
        var orchestrator = new AppStartupOrchestrator(initializer, logger);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await orchestrator.StartAsync());

        Assert.That(ex, Is.Not.Null);
        Assert.That(initializer.Calls, Is.EqualTo(1));
        Assert.That(logger.Entries.Any(e => e.Level == LogLevel.Error && e.Message.Contains("Startup initialization failed.")), Is.True);
    }

    private sealed class TestStartupInitializer : IAppStartupInitializer
    {
        private readonly bool shouldThrow;

        public TestStartupInitializer(bool shouldThrow)
        {
            this.shouldThrow = shouldThrow;
        }

        public int Calls { get; private set; }

        public Task InitializeAsync()
        {
            Calls++;
            if (shouldThrow)
            {
                throw new InvalidOperationException("boom");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
