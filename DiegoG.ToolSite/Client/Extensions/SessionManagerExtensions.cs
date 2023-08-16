namespace DiegoG.ToolSite.Client.Extensions;

public static class SessionManagerExtensions
{
    public static HttpRequestMessage CreatePostMessage(this SessionManager session, string uri, HttpClient? http = null)
        => session.CreateMessage(HttpMethod.Post, uri, http);

    public static HttpRequestMessage CreateGetMessage(this SessionManager session, string uri, HttpClient? http = null)
        => session.CreateMessage(HttpMethod.Get, uri, http);

    public static HttpRequestMessage CreateDeleteMessage(this SessionManager session, string uri, HttpClient? http = null)
        => session.CreateMessage(HttpMethod.Delete, uri, http);

    public static HttpRequestMessage CreatePutMessage(this SessionManager session, string uri, HttpClient? http = null)
        => session.CreateMessage(HttpMethod.Put, uri, http);

    public static HttpRequestMessage CreatePatchMessage(this SessionManager session, string uri, HttpClient? http = null)
        => session.CreateMessage(HttpMethod.Patch, uri, http);
}
