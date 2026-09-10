namespace CoreApp.Application.Abstractions.Sync;

/// <summary>Provides the stable identifier of this application installation.</summary>
public interface ISyncDeviceIdentity
{
    /// <summary>Gets the durable installation identifier.</summary>
    string DeviceId { get; }
}
