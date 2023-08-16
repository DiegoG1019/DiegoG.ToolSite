using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Server.Database;
using DiegoG.ToolSite.Server.Database.Models.Base;
using DiegoG.ToolSite.Server.Models;
using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Controllers;

public class ToolSiteAuthenticatedController : ToolSiteController
{
    private Id<User>? _user;

    [FromServices]
    protected UserManager UserManager { get; set; }

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