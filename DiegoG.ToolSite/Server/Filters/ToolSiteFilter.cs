using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Filters;

public abstract class ToolSiteFilter
{
    protected virtual ILogger CreateLogger(HttpContext context)
        => LogHelper.CreateLogger(
            "Filters",
            GetType().Name,
            "[Uri: {Scheme}://{Host}{Uri}] [Method: {Method}] [Trace: {Trace}]",
            new LogProperty("Controller", GetType().Name),
            new LogProperty("Trace", context.TraceIdentifier),
            new LogProperty("Uri", context.Request.Path.ToString()),
            new LogProperty("Host", context.Request.Host.ToString()),
            new LogProperty("Scheme", context.Request.Scheme),
            new LogProperty("Method", context.Request.Method)
        );
}
