namespace MothballMobile.Infrastructure.Sync;

/// <summary>Stores the synchronization access token using platform secure storage.</summary>
public sealed class SecureSyncTokenProvider(ISecureStorage secureStorage) : ISyncTokenProvider
{
    private const string AccessTokenKey = "Sync.AccessToken";

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => await secureStorage.GetAsync(AccessTokenKey).WaitAsync(cancellationToken).ConfigureAwait(false);

    public Task SetAccessTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("Token is required.", nameof(token));
        return secureStorage.SetAsync(AccessTokenKey, token.Trim()).WaitAsync(cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        secureStorage.Remove(AccessTokenKey);
        return Task.CompletedTask;
    }
}
