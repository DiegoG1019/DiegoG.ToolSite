using System.Collections.Concurrent; 
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using DiegoG.ToolSite.Shared.Types;

namespace DiegoG.ToolSite.Client.Types;

public class HttpCachingHandler : HttpClientHandler
{
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public string? AuthParam { get; }
        public string? AuthScheme { get; }
        public string Uri { get; }

        public CacheKey(AuthenticationHeaderValue? auth, string uri)
            : this(auth?.Parameter, auth?.Scheme, uri)
        { }

        public CacheKey(string? authParam, string? authScheme, string uri)
        {
            AuthParam = authParam;
            AuthScheme = authScheme;
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public bool Equals(CacheKey other)
            => string.Equals(AuthParam, other.AuthParam, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(AuthScheme, other.AuthScheme, StringComparison.CurrentCultureIgnoreCase)
                && string.Equals(Uri, other.Uri, StringComparison.CurrentCultureIgnoreCase);

        public override bool Equals(object? obj) 
            => obj is CacheKey key && Equals(key);

        public override int GetHashCode()
            => HashCode.Combine(
                string.GetHashCode(AuthParam ?? "", StringComparison.CurrentCultureIgnoreCase),
                string.GetHashCode(AuthScheme ?? "", StringComparison.CurrentCultureIgnoreCase),
                string.GetHashCode(Uri, StringComparison.CurrentCultureIgnoreCase)
            );
    }

    private readonly record struct CacheItem(
        DateTime Expiration,
        byte[]? Data,
        HttpResponseHeaders Headers,
        HttpResponseHeaders TrailingHeaders,
        HttpStatusCode StatusCode,
        Version Version,
        string? ReasonPhrase
    );

    private readonly ConcurrentDictionary<CacheKey, CacheItem> Cache = new();

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
        if (request.Method != HttpMethod.Get)
            return false;

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

        Cache.TryAdd(new(request.Headers.Authorization, request.RequestUri.AbsolutePath), cached);

        return true;
    }

    private HttpResponseMessage? CheckCache(HttpRequestMessage request)
    {
        if (request.Method != HttpMethod.Get)
            return null;
        if (string.IsNullOrWhiteSpace(request.Headers.Authorization?.Parameter) is false)
            return null;

        if (request.RequestUri != null && (Cache.TryGetValue(new(request.Headers.Authorization, request.RequestUri.AbsolutePath), out var cached)))
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
                Cache.TryRemove(new(request.Headers.Authorization, request.RequestUri.AbsolutePath), out _);
        }

        return null;
    }
}
