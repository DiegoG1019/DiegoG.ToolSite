using DiegoG.ToolSite.Shared;

namespace DiegoG.ToolSite.Server.Workers;

[RegisterToolSiteWorker]
public class BackgroundTaskSweeper : ApiWorker
{
    public override async Task Work(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken);
        await BackgroundTaskStore.Sweep();
    }
}
