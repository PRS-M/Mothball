using CoreApp.Application.Utilities;

namespace Infrastructure.Services.JsonStore;

public sealed class JsonStoreStartupInitializer : IAppStartupInitializer
{
    private readonly JsonInventoryStore store;
    private readonly IFileHandler? files;

    public JsonStoreStartupInitializer(JsonInventoryStore store, IFileHandler? files = null)
    {
        this.store = store;
        this.files = files;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var recovered = await store.TryRecoverAsync(cancellationToken).ConfigureAwait(false);
        if (!recovered)
        {
            throw new InvalidOperationException("Failed to recover JSON inventory store during startup.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (files is not null && !files.FileExists("seeded-container.jpg", Constants.PathToSharedPhotos))
        {
            await files.CopyFileFromRawToAppDataAsync(
                "container.png",
                "seeded-container.jpg",
                Constants.PathToSharedPhotos);
        }
    }
}
