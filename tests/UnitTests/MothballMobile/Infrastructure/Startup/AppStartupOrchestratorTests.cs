using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CoreApp.Application.Utilities;
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
    public async Task StartAsync_ForwardsCancellationTokenToInitializer()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var cancellationToken = new CancellationTokenSource().Token;
        var observedToken = CancellationToken.None;
        initializer
            .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(token => observedToken = token)
            .Returns(Task.CompletedTask);
        var orchestrator = new AppStartupOrchestrator(
            initializer.Object,
            NullLogger<AppStartupOrchestrator>.Instance);

        await orchestrator.StartAsync(cancellationToken: cancellationToken);

        Assert.That(observedToken, Is.EqualTo(cancellationToken));
    }

    [Test]
    public async Task StartAsync_WhenCalledConcurrently_InitializesOnlyOnce()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        initializer
            .Setup(service => service.InitializeAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                entered.SetResult();
                await release.Task;
            });
        var orchestrator = new AppStartupOrchestrator(
            initializer.Object,
            NullLogger<AppStartupOrchestrator>.Instance);

        var firstStart = orchestrator.StartAsync();
        await entered.Task;
        var secondStart = orchestrator.StartAsync();

        Assert.That(secondStart.IsCompleted, Is.False);
        release.SetResult();
        await Task.WhenAll(firstStart, secondStart);

        initializer.Verify(
            service => service.InitializeAsync(It.IsAny<CancellationToken>()),
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

    [Test]
    public async Task StartAsync_WhenAutomaticDemoSeedingIsDisabled_SkipsSeeder()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var containers = new Mock<IRepository<DbContainer>>();
        var seeder = new DemoDataSeeder(
            containers.Object,
            Mock.Of<IRepository<DbItem>>(),
            Mock.Of<IRepository<DbItemInventory>>(),
            Mock.Of<IRepository<DbImage>>(),
            Mock.Of<IRepository<DbItemContainerRelation>>(),
            Mock.Of<IFileHandler>(),
            NullLogger<DemoDataSeeder>.Instance);
        var orchestrator = new AppStartupOrchestrator(
            initializer.Object,
            NullLogger<AppStartupOrchestrator>.Instance,
            seeder);

        await orchestrator.StartAsync(automaticDemoSeeding: false);

        containers.Verify(repository => repository.GetAllAsync(), Times.Never);
        containers.Verify(repository => repository.InitializeAsync(), Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenDemoSeedIsMarkedCompleteAndDataIsIntact_SkipsHeavySeeding()
    {
        var initializer = new Mock<IAppStartupInitializer>();
        var containers = new Mock<IRepository<DbContainer>>();
        var items = new Mock<IRepository<DbItem>>();
        var inventories = new Mock<IRepository<DbItemInventory>>();
        var photos = new Mock<IRepository<DbImage>>();
        var relations = new Mock<IRepository<DbItemContainerRelation>>();
        var seededContainers = Enumerable.Range(1, 100)
            .Select(index => new DbContainer
            {
                ContainerId = Guid.NewGuid(),
                Name = $"Container {index}",
                Notes = $"Seeded notes [SEED-CONTAINER-MARKER:4f3c5d11-2f9b-44b3-9e55-2e0f1ea7a8d2]",
                BarcodeValue = $"mothball://container/{index}"
            })
            .ToList();

        containers.Setup(repository => repository.InitializeAsync()).Returns(Task.CompletedTask);
        containers.Setup(repository => repository.GetAllAsync()).ReturnsAsync(seededContainers);
        items.Setup(repository => repository.InitializeAsync()).Returns(Task.CompletedTask);
        items.Setup(repository => repository.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<DbItem, bool>>>()))
            .ReturnsAsync(100);
        var fileHandler = new Mock<IFileHandler>();
        fileHandler.Setup(handler => handler.FileExists("seeded-container.jpg", Constants.PathToSharedPhotos))
            .Returns(true);
        fileHandler.Setup(handler => handler.FileExists("seeded-item.jpg", Constants.PathToSharedPhotos))
            .Returns(true);
        var preferences = new Mock<IPreferences>();
        preferences.Setup(store => store.Get("DemoDataSeedVersion", string.Empty))
            .Returns(DemoDataSeeder.SeedVersion);

        var seeder = new DemoDataSeeder(
            containers.Object,
            items.Object,
            inventories.Object,
            photos.Object,
            relations.Object,
            fileHandler.Object,
            NullLogger<DemoDataSeeder>.Instance);
        var orchestrator = new AppStartupOrchestrator(
            initializer.Object,
            Mock.Of<ILogger<AppStartupOrchestrator>>(),
            seeder,
            preferences.Object);

        await orchestrator.StartAsync();

        items.Verify(repository => repository.GetAllAsync(), Times.Never);
        inventories.Verify(repository => repository.InitializeAsync(), Times.Never);
        photos.Verify(repository => repository.InitializeAsync(), Times.Once);
        relations.Verify(repository => repository.InitializeAsync(), Times.Never);
        preferences.Verify(store => store.Set("DemoDataSeedVersion", It.IsAny<string>()), Times.Never);
    }
}
