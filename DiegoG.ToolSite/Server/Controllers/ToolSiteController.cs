using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Shared.Services;
using DiegoG.ToolSite.Server.Database;

namespace DiegoG.ToolSite.Server.Controllers;

public class ToolSiteController : Controller
{
    private ILogger? _log;
    protected ILogger Log => _log ??= CreateLogger();

    [FromServices]
    public ToolSiteContext Db { get; init; }

    protected virtual ILogger CreateLogger()
        => LogHelper.CreateLogger(
            "Controllers",
            GetType().Name,
            "[Uri: {Scheme}://{Host}{Uri}] [Method: {Method}] [Trace: {Trace}]",
            new LogProperty("Controller", GetType().Name),
            new LogProperty("Trace", HttpContext.TraceIdentifier),
            new LogProperty("Uri", HttpContext.Request.Path.ToString()),
            new LogProperty("Host", HttpContext.Request.Host.ToString()),
            new LogProperty("Scheme", HttpContext.Request.Scheme),
            new LogProperty("Method", HttpContext.Request.Method)
        );
}