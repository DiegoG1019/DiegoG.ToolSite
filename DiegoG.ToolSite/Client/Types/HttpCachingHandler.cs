using System.Collections.Concurrent; 
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Client.Types;

public class HttpCachingHandler : HttpClientHandler
{
    private readonly record struct CacheItem(
        DateTime Expiration,
        byte[]? Data,
        HttpResponseHeaders Headers,
        HttpResponseHeaders TrailingHeaders,
        HttpStatusCode StatusCode,
        Version Version,
        string? ReasonPhrase
    );

    private readonly static ConcurrentDictionary<string, CacheItem> PublicCache = new(InvariantCaseInsensitiveStringComparer.Instance);
    private readonly ConcurrentDictionary<string, CacheItem> Cache = new(InvariantCaseInsensitiveStringComparer.Instance);

    [UnsupportedOSPlatform("browser")]
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cached = CheckCache(request);
        if (cached is not null) return cached;

        var newresponse = base.Send(request, cancellationToken);
        TryAddToCache(newresponse, request).ConfigureAwait(false).GetAwaiter().GetResult();
        return newresponse;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cached = CheckCache(request);
        if (cached is not null) return cached;

        var newresponse = await base.SendAsync(request, cancellationToken);
        await TryAddToCache(newresponse, request);
        return newresponse;
    }

    private async ValueTask<bool> TryAddToCache(HttpResponseMessage response, HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(request.Headers.Authorization?.Parameter) is false || string.IsNullOrWhiteSpace(request.RequestUri?.AbsolutePath))
            return false;

        var cc = response.Headers.CacheControl;
        if (cc is null || cc.NoCache || (cc.Public is false && cc.Private is false) || ((cc.MaxAge ?? cc.SharedMaxAge) is null))
            return false;

        var content = await response.Content.ReadAsByteArrayAsync();
        var newcontent = content is not null ? new ByteArrayContent(content) : null;
        response.Content = newcontent;

        var cached = new CacheItem(
            DateTime.Now + (cc.MaxAge ?? cc.SharedMaxAge)!.Value, // we know that one of them is for sure not null, thanks to the check above
            content,
            response.Headers,
            response.TrailingHeaders,
            response.StatusCode,
            response.Version,
            response.ReasonPhrase
        );

        if (cc.Private)
            Cache.TryAdd(request.RequestUri.AbsolutePath, cached);
        else
            PublicCache.TryAdd(request.RequestUri.AbsolutePath, cached);

        return true;
    }

    private HttpResponseMessage? CheckCache(HttpRequestMessage request)
    {
        if (string.IsNullOrWhiteSpace(request.Headers.Authorization?.Parameter) is false)
            return null;

        if (request.RequestUri != null 
            && (Cache.TryGetValue(request.RequestUri.AbsolutePath, out var cached)
                || PublicCache.TryGetValue(request.RequestUri.AbsolutePath, out cached)))
        {
            if (cached.Expiration > DateTime.Now)
            {
                var msg = new HttpResponseMessage()
                {
                    Content = cached.Data is not null ? new ByteArrayContent(cached.Data) : null,
                    ReasonPhrase = cached.ReasonPhrase,
                    RequestMessage = request,
                    StatusCode = cached.StatusCode,
                    Version = cached.Version
                };

                foreach (var header in cached.Headers)
                    msg.Headers.Add(header.Key, header.Value);

                foreach (var trailing in cached.TrailingHeaders)
                    msg.TrailingHeaders.Add(trailing.Key, trailing.Value);

                return msg;
            }
            else
                Cache.TryRemove(request.RequestUri.AbsolutePath, out _);
        }

        return null;
    }
}
