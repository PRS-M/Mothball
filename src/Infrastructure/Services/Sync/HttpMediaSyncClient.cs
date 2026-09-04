using System.Net;
using CoreApp.Application.Features.Media;
using CoreApp.Application.Features.Sync;

namespace Infrastructure.Services.Sync;

/// <summary>Transfers media binaries by immutable content hash over the synchronization endpoint.</summary>
public sealed class HttpMediaSyncClient(
    HttpClient httpClient,
    Uri endpoint,
    ISyncTokenProvider? tokenProvider = null,
    IDeviceIdentityProvider? deviceIdentity = null) : IMediaSyncClient
{
    public async Task UploadAsync(MediaSyncMetadata metadata, Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(content);
        using var request = await CreateRequestAsync(HttpMethod.Put, metadata, cancellationToken).ConfigureAwait(false);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(metadata.MimeType);
        await SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> DownloadAsync(MediaSyncMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        using var request = await CreateRequestAsync(HttpMethod.Get, metadata, cancellationToken).ConfigureAwait(false);
        var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, MediaSyncMetadata metadata, CancellationToken cancellationToken)
    {
        var path = $"workspaces/{metadata.WorkspaceId:D}/media/{Uri.EscapeDataString(metadata.ContentHash)}";
        var request = new HttpRequestMessage(method, new Uri(endpoint, path));
        request.Headers.Add("X-Correlation-Id", Guid.NewGuid().ToString("N"));
        if (deviceIdentity is not null)
            request.Headers.Add("X-Device-Id", deviceIdentity.GetDeviceId().ToString("D"));
        if (tokenProvider is not null)
        {
            var token = await tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        return request;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
                return response;

            var kind = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => SyncErrorKind.Authentication,
                HttpStatusCode.Forbidden => SyncErrorKind.Authorization,
                HttpStatusCode.NotFound => SyncErrorKind.Validation,
                HttpStatusCode.Conflict => SyncErrorKind.Conflict,
                HttpStatusCode.TooManyRequests => SyncErrorKind.RateLimited,
                _ when (int)response.StatusCode >= 500 => SyncErrorKind.Unavailable,
                _ => SyncErrorKind.Validation,
            };
            response.Dispose();
            throw new SyncException(kind, $"Media endpoint returned {(int)response.StatusCode} status.");
        }
        catch (SyncException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { throw new SyncException(SyncErrorKind.Transport, "Media transport failed.", ex); }
    }
}
