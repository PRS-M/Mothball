namespace CoreApp.Application.Abstractions.Platform;

/// <summary>Provides a stable local device identifier; it is not an authentication credential.</summary>
public interface IDeviceIdentityProvider
{
    Guid GetDeviceId();
}
