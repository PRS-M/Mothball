using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure.Startup;

public sealed class AppStartupOrchestrator : IAppStartupOrchestrator
{
    private readonly IAppStartupInitializer startupInitializer;
    private readonly ILogger<AppStartupOrchestrator> logger;
    private readonly DemoDataSeeder? demoSeeder;

    public AppStartupOrchestrator(
        IAppStartupInitializer startupInitializer,
        ILogger<AppStartupOrchestrator> logger,
        DemoDataSeeder? demoSeeder = null)
    {
        this.startupInitializer = startupInitializer;
        this.logger = logger;
        this.demoSeeder = demoSeeder;
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        try
        {
            await startupInitializer.InitializeAsync();
            if (demoSeeder is not null)
            {
                await demoSeeder.EnsureContainersAsync(minContainers: 5, withPhotos: true);
                await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 3, withPhotos: true);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed.");
            throw;
        }
    }
}
