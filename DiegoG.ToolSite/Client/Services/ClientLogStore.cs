using Microsoft.AspNetCore.Components;

namespace DiegoG.ToolSite.Client.Services;

public static class ClientLogStore
{
    public static ILogger PageLogger(ComponentBase component)
        => LogHelper.CreateLogger("Page", component.GetType().Name);
}
