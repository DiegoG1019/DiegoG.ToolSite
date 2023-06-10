using System.Net.Http.Headers;
using DiegoG.ToolSite.Shared.Models.Requests;
using DiegoG.ToolSite.Shared.Models.Responses.Base;
using static System.Formats.Asn1.AsnWriter;
using static System.Net.WebRequestMethods;

namespace DiegoG.ToolSite.Client.Services;

public readonly record struct UserSessionInformation(
    AuthenticationHeaderValue Header, 
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

            var header = new AuthenticationHeaderValue("Bearer", resp.AsExpected().SessionId.ToString());
            http.DefaultRequestHeaders.Authorization = header;
            await RefreshLoginInfo_internal(http, header, storage, ct);
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

            if (CurrentUser is not null)
                await Logout_Internal(http, storage);

            var resp = await http.PostInAPIAsync<SuccesfulLoginResponse, LoginRequest>("api/auth", request, ct: ct);

            if (resp.ApiResponse is ErrorResponse error)
                return (false, error.Errors);

            var header = new AuthenticationHeaderValue("Bearer", resp.AsExpected().SessionId.ToString());
            http.DefaultRequestHeaders.Authorization = header;
            await RefreshLoginInfo_internal(http, header, storage, ct);
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

    private async Task RefreshLoginInfo_internal(HttpClient http, AuthenticationHeaderValue? header, IStorageManager storage, CancellationToken ct = default)
    {
        var resp = await http.GetFromAPIAsync<SessionInformationResponse>("api/auth", ct).AsExpected();
        CurrentUser = new(
            header ?? new("Bearer", resp.SessionId.ToString()),
            resp.SessionId,
            resp.LoggedInAs,
            resp.LoggedInSince,
            resp.IsAnonymous,
            resp.Permissions,
            resp.Settings ?? UserSettings.Default
        );

        await storage.Set("session", CurrentUser);
    }

    private async Task Logout_Internal(HttpClient http, IStorageManager storage)
    {
        await http.DeleteFromAPIAsync<NoResultsResponse>("api/auth");
        CurrentUser = null;
        await storage.Remove("session");
    }

    public async Task Logout()
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageManager>();
            await Logout_Internal(http, storage);
        }
    }
}
