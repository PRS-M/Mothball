using System.Threading.Tasks;
using CoreApp.Interfaces;

namespace Infrastructure.Services;

public sealed class SqliteStartupInitializer : IAppStartupInitializer
{
    private readonly MothballDatabase database;

    public SqliteStartupInitializer(MothballDatabase database)
    {
        this.database = database;
    }

    public Task InitializeAsync() => database.InitializeAsync();
}
