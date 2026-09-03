using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MothballMobile;
using MothballMobile.Infrastructure.Scanning;

namespace Mothball.Tests.Unit.Mobile.Infrastructure.Startup;

[TestFixture]
public sealed class AppStartupCoordinatorTests
{
    private ILocalizationService previousLocalization = null!;

    [SetUp]
    public void SetUp()
    {
        Microsoft.Maui.Controls.Application.Current = new Microsoft.Maui.Controls.Application();
        Microsoft.Maui.Controls.Application.Current.Resources["Background"] = new Color();
        previousLocalization = LocalizationManager.Current;
        var localization = new Mock<ILocalizationService>();
        localization.Setup(service => service.Get(It.IsAny<string>()))
            .Returns((string key) => key);
        LocalizationManager.Configure(localization.Object);
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Maui.Controls.Application.Current = null;
        LocalizationManager.Configure(previousLocalization);
    }

    [Test]
    public async Task InitializeAsync_WhenStartupSucceeds_PresentsShellAfterSecretAndOrchestrator()
    {
        var calls = new List<string>();
        var secrets = new Mock<IBackupSignatureSecretProvider>();
        secrets.Setup(service => service.GetOrCreateAsync())
            .Callback(() => calls.Add("secret"))
            .ReturnsAsync("secret");
        var startup = new Mock<IAppStartupOrchestrator>();
        startup.Setup(service => service.StartAsync())
            .Callback(() => calls.Add("startup"))
            .Returns(Task.CompletedTask);
        var coordinator = CreateCoordinator(secrets.Object, startup.Object);
        var window = new Window(coordinator.CreateStartupPage());

        await coordinator.InitializeAsync(window);

        Assert.Multiple(() =>
        {
            Assert.That(window.Page, Is.TypeOf<AppShell>());
            Assert.That(calls, Is.EqualTo(new[] { "secret", "startup" }));
        });
    }

    [Test]
    public async Task InitializeAsync_WhenStartupFails_PresentsRetryPageAndLogsError()
    {
        var startup = new Mock<IAppStartupOrchestrator>();
        startup.Setup(service => service.StartAsync())
            .ThrowsAsync(new InvalidOperationException("startup failed"));
        var logger = new Mock<ILogger<AppStartupCoordinator>>();
        var coordinator = CreateCoordinator(Mock.Of<IBackupSignatureSecretProvider>(), startup.Object, logger.Object);
        var window = new Window(coordinator.CreateStartupPage());

        await coordinator.InitializeAsync(window);

        Assert.That(window.Page, Is.TypeOf<ContentPage>());
        logger.Verify(
            service => service.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("Application startup failed.")),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task InitializeAsync_WhenSecretInitializationFails_PresentsRetryPageAndSkipsStartup()
    {
        var secrets = new Mock<IBackupSignatureSecretProvider>();
        secrets.Setup(service => service.GetOrCreateAsync())
            .ThrowsAsync(new InvalidOperationException("secret failed"));
        var startup = new Mock<IAppStartupOrchestrator>();
        var coordinator = CreateCoordinator(secrets.Object, startup.Object);
        var window = new Window(coordinator.CreateStartupPage());

        await coordinator.InitializeAsync(window);

        Assert.That(window.Page, Is.TypeOf<ContentPage>());
        startup.Verify(service => service.StartAsync(), Times.Never);
    }

    [Test]
    public async Task StartupErrorPage_WhenRetryClicked_RunsStartupAgain()
    {
        var startup = new Mock<IAppStartupOrchestrator>();
        startup.SetupSequence(service => service.StartAsync())
            .ThrowsAsync(new InvalidOperationException("first attempt"))
            .Returns(Task.CompletedTask);
        var coordinator = CreateCoordinator(Mock.Of<IBackupSignatureSecretProvider>(), startup.Object);
        var window = new Window(coordinator.CreateStartupPage());

        await coordinator.InitializeAsync(window);
        var errorPage = (ContentPage)window.Page!;
        var layout = (VerticalStackLayout)errorPage.Content!;
        var retryButton = layout.Children.OfType<Button>().Single();

        retryButton.RaiseClicked();
        await Task.Delay(50);

        Assert.Multiple(() =>
        {
            Assert.That(startup.Invocations.Count(invocation => invocation.Method.Name == nameof(IAppStartupOrchestrator.StartAsync)), Is.EqualTo(2));
            Assert.That(window.Page, Is.TypeOf<AppShell>());
            Assert.That(retryButton.IsEnabled, Is.False);
        });
    }

    private static AppStartupCoordinator CreateCoordinator(
        IBackupSignatureSecretProvider secrets,
        IAppStartupOrchestrator startup,
        ILogger<AppStartupCoordinator>? logger = null)
        => new(
            startup,
            secrets,
            Mock.Of<IPopupService>(),
            new AdMobSettings(string.Empty, string.Empty),
            logger ?? NullLogger<AppStartupCoordinator>.Instance,
            NullLogger<AppShell>.Instance,
            new BarcodeLookupCoordinator(
                Mock.Of<IBarcodeScanSession>(),
                Mock.Of<IInventoryQueryRepository>(),
                Mock.Of<INavigationService>()));
}
