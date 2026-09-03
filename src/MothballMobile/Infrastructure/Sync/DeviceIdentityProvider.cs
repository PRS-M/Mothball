namespace MothballMobile.Infrastructure.Sync;

/// <summary>Persists a stable per-installation device identifier.</summary>
public sealed class DeviceIdentityProvider(IPreferences preferences) : IDeviceIdentityProvider
{
    private const string DeviceIdKey = "Sync.DeviceId";

    public Guid GetDeviceId()
    {
        var raw = preferences.Get(DeviceIdKey, string.Empty);
        if (Guid.TryParse(raw, out var existing) && existing != Guid.Empty)
            return existing;

        var created = Guid.NewGuid();
        preferences.Set(DeviceIdKey, created.ToString("D"));
        return created;
    }
}
