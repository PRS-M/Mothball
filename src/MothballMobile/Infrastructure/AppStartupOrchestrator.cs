using CoreApp.Interfaces;
using Microsoft.Extensions.Logging;

namespace MothballMobile.Infrastructure;

public sealed class AppStartupOrchestrator : IAppStartupOrchestrator
{
    private readonly IAppStartupInitializer startupInitializer;
    private readonly ILogger<AppStartupOrchestrator> logger;

    public AppStartupOrchestrator(
        IAppStartupInitializer startupInitializer,
        ILogger<AppStartupOrchestrator> logger)
    {
        this.startupInitializer = startupInitializer;
        this.logger = logger;
    }

    public async Task StartAsync()
    {
        try
        {
            await startupInitializer.InitializeAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed.");
        }
    }
}
