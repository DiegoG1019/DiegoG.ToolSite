using Microsoft.AspNetCore.Components;

namespace DiegoG.ToolSite.Client.Pages;

public class ToolSiteComponent : ComponentBase
{
    private ILogger? _log;
    protected ILogger Log => _log ??= ClientLogStore.PageLogger(this);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Log.Information("Initialized Page");
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender);
        if (firstRender)
            Log.Information("Rendered Page for the first time");
        else
            Log.Debug("Rendered Page");
    }
}
