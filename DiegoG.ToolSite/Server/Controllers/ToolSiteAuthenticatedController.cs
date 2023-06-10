using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Database;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Models;
using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Controllers;

public class ToolSiteAuthenticatedController : ToolSiteController
{
    private readonly static TimedCache<Id<User>, User> UserCache = new((k, v) => TimeSpan.FromSeconds(30));

    [FromServices]
    public ToolSiteContext Db { get; init; }

    private Id<User>? _user;

    public Id<User> SiteUserId
    {
        get
        {
            if (_user is not Id<User> u)
            {
                var id = HttpContext.Features.Get<Id<User>>();
                _user = id == default ? throw new InvalidDataException("There is no user available for this page's request") : id;
                return id;
            }
            return u;
        }
    }

    protected async ValueTask<User> FetchSiteUser()
        => await UserCache.GetOrAddAsync(
            SiteUserId,
            async k => await Db.Users.FindAsync(k) ?? throw new InvalidDataException($"The user Id '{k}' did not match any users")
        );

    private Session? _session;
    public Session Session => _session ??= HttpContext.Features.Get<Session>() ?? throw new InvalidDataException("There is no session available for this page's request");

    protected override ILogger CreateLogger()
        => LogHelper.CreateLogger(
            "Controllers",
            GetType().Name,
            null,
            new LogProperty("ControllerName", GetType().Name),
            new LogProperty("UserId", SiteUserId),
            new LogProperty("SessionId", Session?.Id)
        );
}