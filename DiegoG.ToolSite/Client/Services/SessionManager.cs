using System.Net.Http.Headers;

namespace DiegoG.ToolSite.Client.Services;

[RegisterClientService(ServiceLifetime.Singleton)]
public class SessionManager
{
    private readonly IServiceProvider Services;

    public AuthenticationHeaderValue? AuthorizationHeader { get; private set; }
    public SessionId? Session { get; private set; }
    public string? UserName { get; private set; }
    public DateTimeOffset? UserLoggedInSince { get; private set; }
    public bool IsUserAnonymous { get; private set; }

    public SessionManager(IServiceProvider services)
    {
        Services = services;
    }

    public async Task FetchLoginInformation()
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();

            var resp = await http.GetFromAPIAsync<SessionInformationResponse>("api/auth").AsExpected();
            UserName = resp.LoggedInAs;
            UserLoggedInSince = resp.LoggedInSince;
            IsUserAnonymous = resp.IsAnonymous;
            Session = resp.SessionId;
            AuthorizationHeader = new("Bearer", Session.ToString());
        }
    }

    public async Task Logout()
    {
        using (var scope = Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>();
            await http.GetFromAPIAsync<NoResultsResponse>("api/auth/logout").AsExpected();
        }

        Session = null;
        UserName = null;
        UserLoggedInSince = null;
        IsUserAnonymous = false;
        AuthorizationHeader = null;
    }
}
