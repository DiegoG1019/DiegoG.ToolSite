using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace DiegoG.ToolSite.Server.Middleware;

public class ExceptionLogger : ToolSiteMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionLogger(
        RequestDelegate next
    )
    {
        _next = next;
    }

    /// <summary>
    /// Invokes this Middleware's behavior under <paramref name="context"/>
    /// </summary>
    public async Task Invoke(HttpContext context)
    {
        var log = CreateLogger(context);

        Exception e;
        try
        {
            await _next.Invoke(context);
            var eHandler = context.Features.Get<IExceptionHandlerFeature>();
            if (eHandler?.Error is not Exception excp)
                return;

            e = excp;
        }
        catch (Exception ex)
        {
            log.Fatal(ex, "An uncaught exception ocurred");
            Debugger.Break();
            throw;
        }

        Debugger.Break();
        log.Error(e, "An error ocurred and is being handled");
    }
}