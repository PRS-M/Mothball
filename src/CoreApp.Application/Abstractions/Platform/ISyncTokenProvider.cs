namespace CoreApp.Application.Abstractions.Platform;

/// <summary>Provides an access token for synchronization transport without exposing secure storage.</summary>
public interface ISyncTokenProvider
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task SetAccessTokenAsync(string token, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}
