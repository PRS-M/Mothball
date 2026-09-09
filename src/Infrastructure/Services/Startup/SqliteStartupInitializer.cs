using CoreApp.Application.Utilities;

namespace Infrastructure.Services.Startup;

public sealed class SqliteStartupInitializer : IAppStartupInitializer
{
    private readonly MothballDatabase database;
    private readonly IFileHandler files;

    public SqliteStartupInitializer(MothballDatabase database, IFileHandler files)
    {
        this.database = database;
        this.files = files;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await database.InitializeAsync(cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        await EnsureSharedContainerPhotoAsync();
    }

    private async Task EnsureSharedContainerPhotoAsync()
    {
        if (!files.FileExists("seeded-container.jpg", Constants.PathToSharedPhotos))
        {
            await files.CopyFileFromRawToAppDataAsync(
                "container.png",
                "seeded-container.jpg",
                Constants.PathToSharedPhotos);
        }
    }
}
