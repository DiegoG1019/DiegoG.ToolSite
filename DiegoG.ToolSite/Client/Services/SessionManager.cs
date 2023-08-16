using System.Net.Http.Headers;
using DiegoG.ToolSite.Shared.Models;
using DiegoG.ToolSite.Shared.Models.Requests;
using DiegoG.ToolSite.Shared.Models.Responses.Base;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.WebRequestMethods;

namespace DiegoG.ToolSite.Client.Services;

public readonly record struct UserSessionInformation(
    SessionId Session, 
    string? Username, 
    DateTimeOffset? LoggedInSince,
    bool IsAnonymous,
    UserPermission Permissions,
    UserSettings Settings
);

[RegisterClientService(ServiceLifetime.Singleton)]
public class SessionManager
{
    private readonly IServiceProvider Services;

    public UserSessionInformation? CurrentUser { get; private set; }

    public async ValueTask<UserSessionInformation?> FetchCurrentUser()
    {
        if (CurrentUser is null)
        {
            using var scope = Services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            var (succ, res) = await storage.TryGet<UserSessionInformation?>("session");
            if (succ) CurrentUser = res;
        }

        return CurrentUser;
    }

    public SessionManager(IServiceProvider services)
    {
        Services = services;
    }

    public async Task<(bool Success, IEnumerable<string>? Errors)> CreateNewUserAndLogin(NewUserRequest request, CancellationToken ct = default)
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            await Logout_Internal(http, storage);

            var resp = await http.PutInAPIAsync<SuccesfulLoginResponse, NewUserRequest>("api/auth/new", request, ct: ct);

            if (resp.ApiResponse is ErrorResponse error)
                return (false, error.Errors);

            await RefreshLoginInfo_internal(http, resp.AsExpected().SessionId, storage, ct);
            return (true, null);
        }
    }

    public async Task<(bool Success, IEnumerable<string>? Errors)> Login(LoginRequest request, CancellationToken ct = default)
    {
        using (var scope = Services.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            {
                var (succ, val) = await storage.TryGet<UserSessionInformation?>("session");
                if (succ)
                {
                    CurrentUser = val;
                    return (true, null);
                }
            }

            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

            await Logout_Internal(http, storage);

            var resp = await http.PostInAPIAsync<SuccesfulLoginResponse, LoginRequest>("api/auth", request, ct: ct);

            if (resp.ApiResponse is ErrorResponse error)
                return (false, error.Errors);

            await RefreshLoginInfo_internal(http, resp.AsExpected().SessionId, storage, ct);
            return (true, null);
        }
    }

    public async Task RefreshLoginInformation(CancellationToken ct = default)
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            await RefreshLoginInfo_internal(http, null, storage, ct);
        }
    }

    private async Task RefreshLoginInfo_internal(HttpClient http, SessionId? sessionId, IStorageManager storage, CancellationToken ct = default)
    {
        using var msg = new HttpRequestMessage(HttpMethod.Get, "api/auth");

        if (sessionId is SessionId sid)
            msg.Headers.Add("Authorization", $"Bearer {sid}");
        var resp = await http.SendToAPIAsync<SessionInformationResponse>(msg, ct).AsExpected();

        CurrentUser = new(
            resp.SessionId,
            resp.LoggedInAs,
            resp.LoggedInSince,
            resp.IsAnonymous,
            resp.Permissions,
            resp.Settings ?? UserSettings.Default
        );

        await storage.Set("session", CurrentUser);
    }

    private async Task<bool> Logout_Internal(HttpClient http, IStorageManager storage)
    {
        if (CurrentUser is not null)
        {
            using (var msg = CreateMessage(HttpMethod.Delete, "api/auth", http))
            {
                await http.SendToAPIAsync<NoResultsResponse>(msg).AsExpected();
                CurrentUser = null;
                await storage.Remove("session");
            }
            return true;
        }
        return false;
    }

    public async Task<bool> Logout()
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            return await Logout_Internal(http, storage);
        }
    }

    private static HttpRequestMessage CreateUnauthorizedMessage(HttpMethod method, string uri, HttpClient? client = null)
    {
        var msg = new HttpRequestMessage(method, uri);

        if (client is not null)
        {
            msg.VersionPolicy = client.DefaultVersionPolicy;
            msg.Version = client.DefaultRequestVersion;
        }

        return msg;
    }

    public HttpRequestMessage CreateMessage(HttpMethod method, string uri, HttpClient? client = null)
    {
        var msg = CreateUnauthorizedMessage(method, uri, client);
        if (CurrentUser?.Session is SessionId sid)
            msg.Headers.Add("Authorization", $"Bearer {sid}");
        return msg;
    }
}
