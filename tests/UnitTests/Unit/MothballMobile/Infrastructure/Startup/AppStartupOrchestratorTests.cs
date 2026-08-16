using Microsoft.Extensions.Logging;
using Moq;
using MothballMobile.Infrastructure;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Startup;

[TestFixture]
public class AppStartupOrchestratorTests
{
    [Test]
    public async Task StartAsync_WhenInitializerSucceeds_CompletesWithoutError()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var logger = new Mock<ILogger<AppStartupOrchestrator>>();
        var orchestrator = new AppStartupOrchestrator(initializer.Object, logger.Object);

        Assert.DoesNotThrowAsync(async () => await orchestrator.StartAsync());

        initializer.Verify(i => i.InitializeAsync(), Times.Once);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenInitializerFails_LogsAndRethrows()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        initializer.Setup(i => i.InitializeAsync()).ThrowsAsync(new InvalidOperationException("boom"));

        var logger = new Mock<ILogger<AppStartupOrchestrator>>();
        var orchestrator = new AppStartupOrchestrator(initializer.Object, logger.Object);

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await orchestrator.StartAsync());
        Assert.That(ex, Is.Not.Null);
        Assert.That(ex!.Message, Is.EqualTo("boom"));

        initializer.Verify(i => i.InitializeAsync(), Times.Once);
        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Startup initialization failed.")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
