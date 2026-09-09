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
    private readonly SemaphoreSlim startupGate = new(1, 1);
    private bool startupCompleted;

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
    public async Task StartAsync(
        IProgress<StartupProgress>? progress = null,
        bool automaticDemoSeeding = true,
        CancellationToken cancellationToken = default)
    {
        await startupGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (startupCompleted)
            {
                return;
            }

            progress?.Report(new StartupProgress(0, 0, "Preparing startup"));
            await startupInitializer.InitializeAsync(cancellationToken);
            progress?.Report(new StartupProgress(0.2, 0, "Initializing local data"));

            if (automaticDemoSeeding && demoSeeder is not null && await ShouldRunDemoSeedAsync(demoSeeder, progress, cancellationToken))
            {
                var containerProgress = new Progress<double>(fraction =>
                    progress?.Report(new StartupProgress(0.2 + fraction * 0.2, fraction, "Generating demo containers")));
                await demoSeeder.EnsureContainersAsync(minContainers: 100, withPhotos: true, containerProgress, cancellationToken);

                var itemProgress = new Progress<double>(fraction =>
                    progress?.Report(new StartupProgress(0.4 + fraction * 0.45, fraction, "Generating demo items")));
                await demoSeeder.EnsureItemsAsync(minItemsPerContainer: 100, withPhotos: true, itemProgress, cancellationToken);

                if (preferences is not null && await demoSeeder.IsSeedDataIntactAsync(100, 100, cancellationToken))
                {
                    preferences.Set(DemoSeedVersionKey, DemoDataSeeder.SeedVersion);
                }
            }
            else if (demoSeeder is not null)
            {
                logger.LogDebug("Skipping demo data seeding because version {SeedVersion} is already complete.", DemoDataSeeder.SeedVersion);
            }

            progress?.Report(new StartupProgress(0.85, 1, "Preparing application"));
            startupCompleted = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Startup initialization failed.");
            throw;
        }
        finally
        {
            startupGate.Release();
        }
    }

    private async Task<bool> ShouldRunDemoSeedAsync(
        DemoDataSeeder seeder,
        IProgress<StartupProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (preferences is null ||
            !string.Equals(
                preferences.Get(DemoSeedVersionKey, string.Empty),
                DemoDataSeeder.SeedVersion,
                StringComparison.Ordinal))
        {
            return true;
        }

        progress?.Report(new StartupProgress(0.2, 0, "Checking demo data"));
        return !await seeder.IsSeedDataIntactAsync(minContainers: 100, minItemsPerContainer: 100, cancellationToken);
    }
}
