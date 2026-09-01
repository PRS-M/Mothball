using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MothballMobile.Infrastructure;
using Infrastructure.Services.DatabaseModels;

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

    [Test]
    public async Task StartAsync_WhenDemoSeederIsAvailable_SeedsAfterPersistenceInitialization()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var containers = new Mock<IRepository<DbContainer>>();
        var items = new Mock<IRepository<DbItem>>();
        var inventories = new Mock<IRepository<DbItemInventory>>();
        var photos = new Mock<IRepository<DbImage>>();
        var relations = new Mock<IRepository<DbItemContainerRelation>>();
        containers.Setup(repository => repository.GetAllAsync()).ReturnsAsync(
            Enumerable.Range(1, 5)
                .Select(index => new DbContainer
                {
                    ContainerId = Guid.NewGuid(),
                    Name = $"User container {index}",
                })
                .ToList());

        var seeder = new DemoDataSeeder(
            containers.Object,
            items.Object,
            inventories.Object,
            photos.Object,
            relations.Object,
            Mock.Of<IFileHandler>(),
            NullLogger<DemoDataSeeder>.Instance);
        var orchestrator = new AppStartupOrchestrator(
            initializer.Object,
            Mock.Of<ILogger<AppStartupOrchestrator>>(),
            seeder);

        await orchestrator.StartAsync();

        initializer.Verify(service => service.InitializeAsync(), Times.Once);
        containers.Verify(repository => repository.GetAllAsync(), Times.Exactly(2));
        items.Verify(repository => repository.InitializeAsync(), Times.Once);
    }
}
