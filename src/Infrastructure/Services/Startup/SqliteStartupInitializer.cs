using System.Threading.Tasks;

namespace Infrastructure.Services.Startup;

public sealed class SqliteStartupInitializer : IAppStartupInitializer
{
    private readonly MothballDatabase database;

    public SqliteStartupInitializer(MothballDatabase database)
    {
        this.database = database;
    }

    /// <inheritdoc />
    public Task InitializeAsync() => database.InitializeAsync();
}
