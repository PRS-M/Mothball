using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using Infrastructure.Services.Seeding;

namespace MothballMobile.Infrastructure.Startup;

public sealed class AppStartupOrchestrator : IAppStartupOrchestrator
{
    private const string DemoSeedVersionKey = "DemoDataSeedVersion";

    private readonly IAppStartupInitializer startupInitializer;
    private readonly ILogger<AppStartupOrchestrator> logger;
    private readonly DemoDataSeeder? demoSeeder;
    private readonly IPreferences? preferences;

    public AppStartupOrchestrator(
        IAppStartupInitializer startupInitializer,
        ILogger<AppStartupOrchestrator> logger,
        DemoDataSeeder? demoSeeder = null,
        IPreferences? preferences = null)
    {
        this.startupInitializer = startupInitializer;
        this.logger = logger;
        this.demoSeeder = demoSeeder;
        this.preferences = preferences;
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        try
        {
            await startupInitializer.InitializeAsync();
            if (demoSeeder is not null && await ShouldRunDemoSeedAsync(demoSeeder))
            {
                await demoSeeder.EnsureContainersAsync(minContainers: 100, withPhotos: true);
                await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 100, withPhotos: true);
                preferences?.Set(DemoSeedVersionKey, DemoDataSeeder.SeedVersion);
            }
            else if (demoSeeder is not null)
            {
                logger.LogDebug("Skipping demo data seeding because version {SeedVersion} is already complete.", DemoDataSeeder.SeedVersion);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed.");
            throw;
        }
    }

    private async Task<bool> ShouldRunDemoSeedAsync(DemoDataSeeder seeder)
    {
        if (preferences is null ||
            !string.Equals(
                preferences.Get(DemoSeedVersionKey, string.Empty),
                DemoDataSeeder.SeedVersion,
                StringComparison.Ordinal))
        {
            return true;
        }

        return !await seeder.IsSeedDataIntactAsync(minContainers: 100, minItemsPerContainer: 100);
    }
}
