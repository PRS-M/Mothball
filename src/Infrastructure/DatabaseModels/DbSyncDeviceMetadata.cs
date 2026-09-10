using SQLite;

namespace Infrastructure.Services.DatabaseModels;

public sealed class DbSyncDeviceMetadata
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    [NotNull]
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N");
}
