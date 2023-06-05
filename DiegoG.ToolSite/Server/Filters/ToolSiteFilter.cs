using DiegoG.ToolSite.Shared.Services;

namespace DiegoG.ToolSite.Server.Filters;

public abstract class ToolSiteFilter
{
    protected virtual ILogger CreateLogger(HttpContext context)
        => LogHelper.CreateLogger(
            "Filter",
            GetType().Name,
            null,
            new LogProperty("Trace", context.TraceIdentifier)
        );
}
