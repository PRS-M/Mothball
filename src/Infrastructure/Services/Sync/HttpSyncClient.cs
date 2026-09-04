using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CoreApp.Application.Features.Sync;

namespace Infrastructure.Services.Sync;

/// <summary>Optional HTTP adapter for the backend-neutral synchronization client.</summary>
public sealed class HttpSyncClient(HttpClient httpClient, Uri endpoint, ISyncTokenProvider? tokenProvider = null, IDeviceIdentityProvider? deviceIdentity = null) : ISyncClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<SyncBootstrapResult> BootstrapAsync(Guid workspaceId, CancellationToken cancellationToken = default)
        => SendAsync<SyncBootstrapResult>(HttpMethod.Post, $"workspaces/{workspaceId:D}/sync/bootstrap", null, cancellationToken);

    public Task<SyncPushResult> PushAsync(Guid workspaceId, IReadOnlyList<PendingSyncOperation> operations, CancellationToken cancellationToken = default)
        => SendAsync<SyncPushResult>(HttpMethod.Post, $"workspaces/{workspaceId:D}/sync/push", operations, cancellationToken);

    public Task<SyncChangePage> PullAsync(Guid workspaceId, string? cursor, int pageSize, CancellationToken cancellationToken = default)
        => SendAsync<SyncChangePage>(HttpMethod.Post, $"workspaces/{workspaceId:D}/sync/pull", new { cursor, pageSize }, cancellationToken);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri(endpoint, path));
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        if (deviceIdentity is not null) request.Headers.Add("X-Device-Id", deviceIdentity.GetDeviceId().ToString("D"));
        if (tokenProvider is not null)
        {
            var token = await tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        if (body is not null) request.Content = JsonContent.Create(body, options: JsonOptions);
        try
        {
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var kind = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => SyncErrorKind.Authentication,
                    HttpStatusCode.Forbidden => SyncErrorKind.Authorization,
                    HttpStatusCode.Conflict => SyncErrorKind.Conflict,
                    HttpStatusCode.TooManyRequests => SyncErrorKind.RateLimited,
                    _ when (int)response.StatusCode >= 500 => SyncErrorKind.Unavailable,
                    _ => SyncErrorKind.Validation,
                };
                throw new SyncException(kind, $"Synchronization endpoint returned {(int)response.StatusCode}.");
            }

            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return result ?? throw new SyncException(SyncErrorKind.Validation, "Synchronization endpoint returned an empty response.");
        }
        catch (SyncException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { throw new SyncException(SyncErrorKind.Transport, "Synchronization transport failed.", ex); }
    }
}
