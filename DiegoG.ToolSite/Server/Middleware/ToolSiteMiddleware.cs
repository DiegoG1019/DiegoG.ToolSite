using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Middleware;

public abstract class ToolSiteMiddleware
{
    protected virtual ILogger CreateLogger(HttpContext context)
        => LogHelper.CreateLogger(
            "Middleware",
            $"Middleware: {GetType().Name}",
            null,
            new LogProperty("Trace", context.TraceIdentifier)
        );
}
