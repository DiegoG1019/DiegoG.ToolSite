using Microsoft.AspNetCore.Mvc;
using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Controllers;

public class ToolSiteController : Controller
{
    private ILogger? _log;
    protected ILogger Log => _log ??= CreateLogger();

    protected virtual ILogger CreateLogger()
        => LogHelper.CreateLogger(
            "Controllers",
            GetType().Name,
            null,
            new LogProperty("Controller", GetType().Name),
            new LogProperty("Trace", HttpContext.TraceIdentifier)
        );
}